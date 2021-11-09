using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Validator;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Events;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

/// <summary>
/// Its job is to synchronize current peer with connected peers, requiring needed data and parsing them.
/// </summary>
/// <seealso cref="BaseProcessor" />
public partial class SynchronizationProcessor : BaseProcessor, IPeriodicWorkExceptionHandler, IDisposable,
   INetworkMessageHandler<HeadersMessage>,
   INetworkMessageHandler<BlockMessage>
{
   readonly IDateTimeProvider _dateTimeProvider;
   private readonly IConsensusParameters _consensusParameters;
   private readonly IInitialBlockDownloadTracker _ibdState;
   private readonly IBlockHeaderHashCalculator _blockHeaderHashCalculator;
   readonly ITransactionHashCalculator _transactionHashCalculator;
   readonly IBlockFetcherManager _blockFetcherManager;
   readonly ILocalServiceProvider _localServiceProvider;
   readonly IChainState _chainState;
   readonly IHeaderValidator _headerValidator;
   readonly IBlockValidator _blockValidator;
   readonly IPeriodicWork _headerSyncLoop;
   readonly BitcoinSettings _options;
   private readonly Target _minimumChainWork;

   public SynchronizationProcessor(ILogger<SynchronizationProcessor> logger,
                               IEventBus eventBus,
                               IDateTimeProvider dateTimeProvider,
                               IPeerBehaviorManager peerBehaviorManager,
                               IConsensusParameters consensusParameters,
                               IInitialBlockDownloadTracker ibdState,
                               IBlockHeaderHashCalculator blockHeaderHashCalculator,
                               ITransactionHashCalculator transactionHashCalculator,
                               IBlockFetcherManager blockFetcherManager,
                               ILocalServiceProvider localServiceProvider,
                               IChainState chainState,
                               IHeaderValidator headerValidator,
                               IBlockValidator blockValidator,
                               IPeriodicWork headerSyncLoop,
                               IOptions<BitcoinSettings> options)
      : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
   {
      _dateTimeProvider = dateTimeProvider;
      _consensusParameters = consensusParameters;
      _ibdState = ibdState;
      _blockHeaderHashCalculator = blockHeaderHashCalculator;
      _transactionHashCalculator = transactionHashCalculator;
      _blockFetcherManager = blockFetcherManager;
      _localServiceProvider = localServiceProvider;
      _chainState = chainState;
      _headerValidator = headerValidator;
      _blockValidator = blockValidator;
      _headerSyncLoop = headerSyncLoop;
      _options = options.Value;


      _minimumChainWork = _options.MinimumChainWork ?? _consensusParameters.MinimumChainWork;
      if (_minimumChainWork < _consensusParameters.MinimumChainWork)
      {
         this.logger.LogWarning($"{nameof(_minimumChainWork)} set below default value of {_consensusParameters.MinimumChainWork}");
      }

      headerSyncLoop.Configure(stopOnException: false, this);
   }

   public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
   {
      string? disconnectionReason = failedWork switch
      {
         IPeriodicWork work when work == _headerSyncLoop => "Peer header syncing loop had failures.",
         _ => null
      };

      if (disconnectionReason != null)
      {
         feedback.ContinueExecution = false;
         PeerContext.Disconnect(disconnectionReason);
      }
   }

   protected override ValueTask OnPeerAttachedAsync()
   {
      RegisterLifeTimeEventHandler<BlockHeaderValidationSucceeded>(OnBlockHeaderValidationSucceededAsync, arg => arg.PeerContext == PeerContext);
      RegisterLifeTimeEventHandler<BlockHeaderValidationFailed>(OnBlockHeaderValidationFailedAsync, arg => arg.PeerContext == PeerContext);

      RegisterLifeTimeEventHandler<BlockValidationSucceeded>(OnBlockValidationSucceededAsync, arg => arg.PeerContext == PeerContext);
      RegisterLifeTimeEventHandler<BlockValidationFailed>(OnBlockValidationFailedAsync, arg => arg.PeerContext == PeerContext);

      return base.OnPeerAttachedAsync();
   }

   /// <summary>
   /// When the peer handshake, sends <see cref="SendCmpctMessage" />  and <see cref="SendHeadersMessage" /> if the
   /// negotiated protocol allow that and update peer status based on its version message.
   /// </summary>
   /// <returns></returns>
   protected override async ValueTask OnPeerHandshakedAsync()
   {
      HandshakeProcessor.HandshakeProcessorStatus handshakeStatus = PeerContext.Features.Get<HandshakeProcessor.HandshakeProcessorStatus>();

      VersionMessage peerVersion = handshakeStatus.PeerVersion!;

      _status.IsLimitedNode = PeerContext.IsLimitedNode;
      _status.IsClient = PeerContext.IsClient;

      _status.PeerStartingHeight = peerVersion.StartHeight;
      _status.CanServeWitness = PeerContext.CanServeWitness;

      await SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);

      if (IsSupported(KnownVersion.V70014))
      {
         // Tell our peer we are willing to provide version 1 or 2 cmpctblocks.
         // However, we do not request new block announcements using cmpctblock messages.
         // We send this to non-NODE NETWORK peers as well, because they may wish to request compact blocks from us.
         if (_localServiceProvider.HasServices(NodeServices.Witness))
         {
            await SendMessageAsync(new SendCmpctMessage { AnnounceUsingCompactBlock = false, Version = 2 }).ConfigureAwait(false);
         }

         await SendMessageAsync(new SendCmpctMessage { AnnounceUsingCompactBlock = false, Version = 1 }).ConfigureAwait(false);
      }


      // if this peer is able to serve blocks, register it
      if (!_status.IsClient)
      {
         _blockFetcherManager.RegisterFetcher(this);
      }

      // starts the header sync loop
      _ = _headerSyncLoop.StartAsync(
            label: $"{nameof(_headerSyncLoop)}-{PeerContext.PeerId}",
            work: SyncLoopAsync,
            interval: TimeSpan.FromMilliseconds(SYNC_LOOP_INTERVAL),
            cancellation: PeerContext.ConnectionCancellationTokenSource.Token
         );
   }

   private async Task SyncLoopAsync(CancellationToken cancellationToken)
   {
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();

      HeaderNode? bestHeaderNode = _chainState.BestHeader;
      if (!_chainState.TryGetBlockHeader(bestHeaderNode, out BlockHeader? bestBlockHeader))
      {
         ThrowHelper.ThrowNotSupportedException("BestHeader should always be available, this should never happen");
      }

      if (!_status.IsSynchronizingHeaders)
      {
         _status.IsSynchronizingHeaders = true;
         _status.HeadersSyncTimeout =
            _dateTimeProvider.GetTimeMicros()
            + HEADERS_DOWNLOAD_TIMEOUT_BASE
            + HEADERS_DOWNLOAD_TIMEOUT_PER_HEADER * (
               (_dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - bestBlockHeader.TimeStamp) / _consensusParameters.PowTargetSpacing
               );

         /* If possible, start at the block preceding the currently
            best known header.  This ensures that we always get a
            non-empty list of headers back as long as the peer
            is up-to-date.  With a non-empty response, we can initialise
            the peer's known best block.  This wouldn't be possible
            if we requested starting at pindexBestHeader and
            got back an empty response.  */
         HeaderNode? pindexStart = bestHeaderNode.Previous ?? bestHeaderNode;

         logger.LogDebug("Starting syncing headers from height {LocatorHeight} (peer starting height: {StartingHeight})", pindexStart.Height, _status.PeerStartingHeight);

         var newGetHeaderRequest = new GetHeadersMessage
         {
            Version = (uint)PeerContext.NegotiatedProtocolVersion.Version,
            BlockLocator = _chainState.GetLocator(pindexStart),
            HashStop = UInt256.Zero
         };

         await SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);
      }

      CheckSyncStallingLocked(bestBlockHeader);

      ConsiderEviction(_dateTimeProvider.GetTime());
   }

   private void CheckSyncStallingLocked(BlockHeader bestHeader)
   {
      // Check for headers sync timeouts
      if (_status.IsSynchronizingHeaders && _status.HeadersSyncTimeout < long.MaxValue)
      {
         long now = _dateTimeProvider.GetTimeMicros();
         // Detect whether this is a stalling initial-headers-sync peer
         if (bestHeader.TimeStamp <= _dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - 24 * 60 * 60)
         {
            bool isTheOnlyPeerSynching = true; //nSyncStarted == 1 && (nPreferredDownload - state.fPreferredDownload >= 1)
            if (now > _status.HeadersSyncTimeout && isTheOnlyPeerSynching)
            {
               // Disconnect a (non-whitelisted) peer if it is our only sync peer,
               // and we have others we could be using instead.
               // Note: If all our peers are inbound, then we won't
               // disconnect our sync peer for stalling; we have bigger
               // problems if we can't get any outbound peers.
               if (!PeerContext.Permissions.Has(BitcoinPeerPermissions.NOBAN))
               {
                  PeerContext.Disconnect("Timeout downloading headers, disconnecting");
                  return;
               }
               else
               {
                  logger.LogDebug("Timeout downloading headers from whitelisted peer {PeerId}, not disconnecting.", PeerContext.PeerId);
                  // Reset the headers sync state so that we have a
                  // chance to try downloading from a different peer.
                  // Note: this will also result in at least one more
                  // getheaders message to be sent to
                  // this peer (eventually).
                  _status.IsSynchronizingHeaders = false;
                  _status.HeadersSyncTimeout = 0;
               }
            }
         }
         else
         {
            // After we've caught up once, reset the timeout so we can't trigger disconnect later.
            _status.HeadersSyncTimeout = long.MaxValue;
         }
      }
   }

   private void ConsiderEviction(long time)
   {
      //     /** State used to enforce CHAIN_SYNC_TIMEOUT
      //  * Only in effect for outbound, non-manual, full-relay connections, with
      //  * m_protect == false
      //  * Algorithm: if a peer's best known block has less work than our tip,
      //  * set a timeout CHAIN_SYNC_TIMEOUT seconds in the future:
      //  *   - If at timeout their best known block now has more work than our tip
      //  *     when the timeout was set, then either reset the timeout or clear it
      //  *     (after comparing against our current tip's work)
      //  *   - If at timeout their best known block still has less work than our
      //  *     tip did when the timeout was set, then send a getheaders message,
      //  *     and set a shorter timeout, HEADERS_RESPONSE_TIME seconds in future.
      //  *     If their best known block is still behind when that new timeout is
      //  *     reached, disconnect.
      //  */
      //struct ChainSyncTimeoutState
      //  {
      //     //! A timeout used for checking whether our peer has sufficiently synced
      //     int64_t m_timeout;
      //     //! A header with the work we require on our peer's chain
      //     const CBlockIndex* m_work_header;
      //     //! After timeout is reached, set to true after sending getheaders
      //     bool m_sent_getheaders;
      //     //! Whether this peer is protected from disconnection due to a bad/slow chain
      //     bool m_protect;
      //  };

      //L3695
      //if (!state.m_chain_sync.m_protect && IsOutboundDisconnectionCandidate(pto) && state.fSyncStarted)
      //{
      //   // This is an outbound peer subject to disconnection if they don't
      //   // announce a block with as much work as the current tip within
      //   // CHAIN_SYNC_TIMEOUT + HEADERS_RESPONSE_TIME seconds (note: if
      //   // their chain has more work than ours, we should sync to it,
      //   // unless it's invalid, in which case we should find that out and
      //   // disconnect from them elsewhere).
      //   if (state.pindexBestKnownBlock != nullptr && state.pindexBestKnownBlock->nChainWork >= ::ChainActive().Tip()->nChainWork)
      //   {
      //      if (state.m_chain_sync.m_timeout != 0)
      //      {
      //         state.m_chain_sync.m_timeout = 0;
      //         state.m_chain_sync.m_work_header = nullptr;
      //         state.m_chain_sync.m_sent_getheaders = false;
      //      }
      //   }
      //   else if (state.m_chain_sync.m_timeout == 0 || (state.m_chain_sync.m_work_header != nullptr && state.pindexBestKnownBlock != nullptr && state.pindexBestKnownBlock->nChainWork >= state.m_chain_sync.m_work_header->nChainWork))
      //   {
      //      // Our best block known by this peer is behind our tip, and we're either noticing
      //      // that for the first time, OR this peer was able to catch up to some earlier point
      //      // where we checked against our tip.
      //      // Either way, set a new timeout based on current tip.
      //      state.m_chain_sync.m_timeout = time_in_seconds + CHAIN_SYNC_TIMEOUT;
      //      state.m_chain_sync.m_work_header = ::ChainActive().Tip();
      //      state.m_chain_sync.m_sent_getheaders = false;
      //   }
      //   else if (state.m_chain_sync.m_timeout > 0 && time_in_seconds > state.m_chain_sync.m_timeout)
      //   {
      //      // No evidence yet that our peer has synced to a chain with work equal to that
      //      // of our tip, when we first detected it was behind. Send a single getheaders
      //      // message to give the peer a chance to update us.
      //      if (state.m_chain_sync.m_sent_getheaders)
      //      {
      //         // They've run out of time to catch up!
      //         LogPrintf("Disconnecting outbound peer %d for old chain, best known block = %s\n", pto.GetId(), state.pindexBestKnownBlock != nullptr ? state.pindexBestKnownBlock->GetBlockHash().ToString() : "<none>");
      //         pto.fDisconnect = true;
      //      }
      //      else
      //      {
      //         assert(state.m_chain_sync.m_work_header);
      //         LogPrint(BCLog::NET, "sending getheaders to outbound peer=%d to verify chain work (current best known block:%s, benchmark blockhash: %s)\n", pto.GetId(), state.pindexBestKnownBlock != nullptr ? state.pindexBestKnownBlock->GetBlockHash().ToString() : "<none>", state.m_chain_sync.m_work_header->GetBlockHash().ToString());
      //         connman->PushMessage(&pto, msgMaker.Make(NetMsgType::GETHEADERS, ::ChainActive().GetLocator(state.m_chain_sync.m_work_header->pprev), uint256()));
      //         state.m_chain_sync.m_sent_getheaders = true;
      //         constexpr int64_t HEADERS_RESPONSE_TIME = 120; // 2 minutes
      //                                                        // Bump the timeout to allow a response, which could clear the timeout
      //                                                        // (if the response shows the peer has synced), reset the timeout (if
      //                                                        // the peer syncs to the required work but not to our tip), or result
      //                                                        // in disconnect (if we advance to the timeout and pindexBestKnownBlock
      //                                                        // has not sufficiently progressed)
      //         state.m_chain_sync.m_timeout = time_in_seconds + HEADERS_RESPONSE_TIME;
      //      }
      //   }
      //}
   }

   /// <summary>
   /// The peer sent us headers.
   /// </summary>
   async ValueTask<bool> INetworkMessageHandler<HeadersMessage>.ProcessMessageAsync(HeadersMessage headersMessage, CancellationToken cancellation)
   {
      BlockHeader[]? headers = headersMessage.Headers;
      int headersCount = headers!.Length;

      /// https://github.com/bitcoin/bitcoin/blob/b949ac9697a6cfe087f60a16c063ab9c5bf1e81f/src/net_processing.cpp#L2923-L2947
      /// bitcoin does this before deserialize the message but I don't think would be a big problem, we could ban the peer in case we find this being a vector attack.
      if (headersCount > MAX_HEADERS)
      {
         Misbehave(20, "Too many headers received.");
         return false;
      }

      //// Ignore headers received while importing
      //if (fImporting || fReindex)
      //{
      //   this.logger.LogDebug("Unexpected headers message received from peer {RemoteEndPoint}", this.PeerContext.RemoteEndPoint);
      //   return false;
      //}

      return await ProcessHeadersAsync(headers).ConfigureAwait(false);
   }

   /// <summary>
   /// Processes the headers.
   /// It's invoked because of "headers" or "cmpctblock" message
   /// </summary>
   /// <param name="headers">The headers.</param>
   /// <returns></returns>
   private async Task<bool> ProcessHeadersAsync(BlockHeader[] headers)
   {
      int protocolVersion = PeerContext.NegotiatedProtocolVersion.Version;
      int headersCount = headers.Length;

      if (headersCount == 0)
      {
         logger.LogDebug("Peer didn't returned any headers, let's assume we reached its tip.");
         return true;
      }

      using (GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult())
      {
         if (await HandleAsNotConnectingAnnouncementAsync(headers).ConfigureAwait(false))
         {
            // fully handled as non connecting announcement
            return true;
         }

         // compute hashes in parallel to speed up the operation and check sent headers are sequential.
         Parallel.ForEach(headers, header =>
         {
            header.Hash = _blockHeaderHashCalculator.ComputeHash(header, protocolVersion);
         });
      }

      // Ensure headers are consecutive.
      for (int i = 1; i < headersCount; i++)
      {
         if (headers[i].PreviousBlockHash != headers[i - 1].Hash)
         {
            Misbehave(20, "Non continuous headers sequence.");
            return false;
         }
      }

      //enqueue headers for validation
      await _headerValidator.RequestValidationAsync(new HeadersToValidate(headers, PeerContext)).ConfigureAwait(false);

      return true;
   }

   /// <summary>
   /// The node received a block.
   /// </summary>
   async ValueTask<bool> INetworkMessageHandler<BlockMessage>.ProcessMessageAsync(BlockMessage message, CancellationToken cancellation)
   {
      int protocolVersion = PeerContext.NegotiatedProtocolVersion.Version;

      BlockHeader header = message.Block!.Header!;
      header.Hash = _blockHeaderHashCalculator.ComputeHash(header, protocolVersion);

      // compute transaction hashes in parallel to speed up the operation and check sent headers are sequential.
      Parallel.ForEach(message.Block.Transactions!, transaction =>
      {
         transaction.Hash = _transactionHashCalculator.ComputeHash(transaction, protocolVersion);
         transaction.WitnessHash = _transactionHashCalculator.ComputeWitnessHash(transaction, protocolVersion);
      });

      await eventBus.PublishAsync(new BlockReceived(message.Block!, PeerContext, this), cancellation).ConfigureAwait(false);

      //enqueue headers for validation
      await _blockValidator.RequestValidationAsync(new BlockToValidate(message.Block!, PeerContext)).ConfigureAwait(false);

      return true;
   }

   /// <summary>
   /// Called when block header validation failed.
   /// </summary>
   /// <param name="arg">The argument.</param>
   /// <returns></returns>
   private ValueTask OnBlockHeaderValidationFailedAsync(BlockHeaderValidationFailed arg)
   {
      logger.LogDebug("Header Validation failed");
      //this.MisbehaveDuringHeaderValidation(arg.ValidationState, "invalid header received");

      return default;
   }

   /// <summary>
   /// Called when block header validation succeeded.
   /// </summary>
   /// <param name="arg">The argument.</param>
   /// <returns></returns>
   private async ValueTask OnBlockHeaderValidationSucceededAsync(BlockHeaderValidationSucceeded arg)
   {
      logger.LogTrace("Header Validation succeeded");
      HeaderNode? lastValidatedHeaderNode = arg.LastValidatedHeaderNode;

      using (GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult())
      {
         if (_status.UnconnectingHeaderReceived > 0)
         {
            logger.LogTrace("Resetting UnconnectingHeaderReceived, was {UnconnectingHeaderReceived}.", _status.UnconnectingHeaderReceived);
            _status.UnconnectingHeaderReceived = 0;
         }

         UpdateBlockAvailability(lastValidatedHeaderNode!.Hash);

         if (arg.NewHeadersFoundCount > 0 && lastValidatedHeaderNode.ChainWork > _chainState.GetTip().ChainWork)
         {
            // we received the new tip of the chain
            _status.LastBlockAnnouncement = _dateTimeProvider.GetTime();
         }

         if (arg.ValidatedHeadersCount == MAX_HEADERS)
         {
            // We received the maximum number of headers per protocol definition, the peer may have more headers.
            // TODO: optimize: if pindexLast is an ancestor of ::ChainActive().Tip or pindexBestHeader, continue
            // from there instead.
            logger.LogTrace("Request another getheaders from height {BlockLocatorStart} (startingHeight: {StartingHeight}).", lastValidatedHeaderNode.Height, _status.PeerStartingHeight);
            var newGetHeaderRequest = new GetHeadersMessage
            {
               Version = (uint)PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = _chainState.GetLocator(lastValidatedHeaderNode),
               HashStop = UInt256.Zero
            };
            await SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);
         }

         DisconnectPeerIfNotUseful(arg.ValidatedHeadersCount);
      }
   }

   private ValueTask OnBlockValidationFailedAsync(BlockValidationFailed arg)
   {
      logger.LogDebug("Header Validation failed");
      Misbehave(20, $"Invalid block received: {arg.ValidationState}", true);

      return default;
   }

   private ValueTask OnBlockValidationSucceededAsync(BlockValidationSucceeded arg)
   {
      logger.LogTrace("Block {BlockId} Validation succeeded", arg.ValidatedBlock!.Header!.Hash);

      if (arg.IsNewBlock)
      {
         logger.LogTrace("Block Validation succeeded");
         _status.LastBlockTime = _dateTimeProvider.GetTime();
      }
      else
      {
         logger.LogTrace("Block {BlockId} already known.", arg.ValidatedBlock!.Header!.Hash);
      }

      return default;
   }

   /// <summary>
   /// Disconnects the peer if not useful.
   /// </summary>
   /// <param name="headersCount">The headers count.</param>
   /// <returns><see langword="true"/> if the peer is going to be disconnected; otherwise <see langword="false"/>.</returns>
   private bool DisconnectPeerIfNotUseful(int headersCount)
   {
      /// If we're in IBD, we want outbound peers that will serve us a useful chain.
      /// Disconnect peers that are on chains with insufficient work.
      if (_ibdState.IsDownloadingBlocks() && headersCount != MAX_HEADERS)
      {
         // When nCount < MAX_HEADERS_RESULTS, we know we have no more headers to fetch from this peer.
         if (_status.BestKnownHeader != null && _status.BestKnownHeader.ChainWork < _minimumChainWork)
         {
            /// This peer has too little work on their headers chain to help us sync so disconnect if it's using an outbound
            /// slot, unless the peer is whitelisted or addnode.
            /// Note: We compare their tip to nMinimumChainWork (rather than current chain tip) because we won't start block
            /// download until we have a headers chain that has at least nMinimumChainWork, even if a peer has a chain past
            /// our tip, as an anti-DoS measure.
            if (IsOutboundDisconnectionCandidate())
            {
               PeerContext.Disconnect("Outbound peer headers chain has insufficient work.");
               return true;
            }
         }
      }

      return false;
   }

   private bool IsOutboundDisconnectionCandidate()
   {
      // return !(node->fInbound || node->m_manual_connection || node->fFeeler || node->fOneShot);
      //TODO improve, this is wrong because only check if it's outbound actually, see proper check above
      return PeerContext.Direction == PeerConnectionDirection.Outbound;
   }

   /// <summary>
   /// If this looks like it could be a block announcement (headersCount less than MAX_BLOCKS_TO_ANNOUNCE),
   /// use special logic for handling headers that don't connect:
   /// - Send a getheaders message in response to try to connect the chain.
   /// - The peer can send up to MAX_UNCONNECTING_HEADERS in a row that don't connect before giving DoS points
   /// - Once a headers message is received that is valid and does connect, unconnecting header counter gets reset back to 0.
   /// see https://github.com/bitcoin/bitcoin/blob/ceb789cf3a9075729efa07f5114ce0369d8606c3/src/net_processing.cpp#L1658-L1683
   /// </summary>
   /// <returns><see langword="true"/> if it has been fully handled like a block announcement.</returns>
   private async Task<bool> HandleAsNotConnectingAnnouncementAsync(BlockHeader[] headers)
   {
      if (!_chainState.TryGetKnownHeaderNode(headers[0].PreviousBlockHash, out _) && headers.Length < MAX_BLOCKS_TO_ANNOUNCE)
      {
         _status.UnconnectingHeaderReceived++;
         if (_status.UnconnectingHeaderReceived % MAX_UNCONNECTING_HEADERS == 0)
         {
            Misbehave(20, "Exceeded maximum number of received unconnecting headers.");
         }

         // ask again for headers starting from current tip
         var newGetHeaderRequest = new GetHeadersMessage
         {
            Version = (uint)PeerContext.NegotiatedProtocolVersion.Version,
            BlockLocator = _chainState.GetTipLocator(),
            HashStop = UInt256.Zero
         };
         await SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);

         logger.LogTrace("received an unconnecting header, missing {PrevBlock}. Request again headers from {BlockLocator}",
                              headers[0].PreviousBlockHash,
                              newGetHeaderRequest.BlockLocator.BlockLocatorHashes[0]);

         UpdateBlockAvailability(headers[^1].Hash);
         return true;
      }

      return false;
   }

   /// <summary>
   /// Gets the fetch flags.
   /// </summary>
   /// <returns></returns>
   private uint GetFetchFlags()
   {
      uint nFetchFlags = 0;
      if (_localServiceProvider.HasServices(NodeServices.Witness) && _status.CanServeWitness)
      {
         nFetchFlags |= InventoryType.MSG_WITNESS_FLAG;
      }

      return nFetchFlags;
   }

   private bool CanDirectFetch()
   {
      return _chainState.GetTipHeader().TimeStamp > _dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - _consensusParameters.PowTargetSpacing * 20;
   }

   /// <summary>
   /// Updates tracking information about which blocks a peer is assumed to have.
   /// After calling this method, status.BestKnownHeader is guaranteed to be non null.
   /// If we eventually get the headers, even from a different peer, we can use this peer to download blocks.
   /// </summary>
   /// <param name="headerHash">The header hash.</param>
   private void UpdateBlockAvailability(UInt256? headerHash)
   {
      if (headerHash == null) ThrowHelper.ThrowArgumentNullException(nameof(headerHash));

      ProcessBlockAvailability();

      if (_chainState.TryGetKnownHeaderNode(headerHash, out HeaderNode? headerNode) && headerNode.ChainWork > Target.Zero)
      {
         // A better block header was announced.
         if (_status.BestKnownHeader == null || headerNode.ChainWork >= _status.BestKnownHeader.ChainWork)
         {
            _status.BestKnownHeader = headerNode;
         }
      }
      else
      {
         // An unknown block header was announced, assuming it's the best one.
         _status.LastUnknownBlockHash = headerHash;
      }
   }

   /// <summary>
   /// Check whether the last unknown block header a peer advertised is finally known.
   /// </summary>
   /// <remarks>
   /// If <see cref="BlockHeaderProcessorStatus.LastUnknownBlockHash" /> is finally found in the headers tree, it means
   /// it's no longer unknown and we set to null the status property.
   /// </remarks>
   private void ProcessBlockAvailability()
   {
      if (_status.LastUnknownBlockHash != null)
      {
         if (_chainState.TryGetKnownHeaderNode(_status.LastUnknownBlockHash, out HeaderNode? headerNode) && headerNode.ChainWork > Target.Zero)
         {
            if (_status.BestKnownHeader == null || headerNode.ChainWork >= _status.BestKnownHeader.ChainWork)
            {
               _status.BestKnownHeader = headerNode;
            }
            _status.LastUnknownBlockHash = null;
         }
      }
   }

   private bool IsWitnessEnabled(HeaderNode? headerNode)
   {
      return (headerNode?.Height ?? 0) + 1 >= _consensusParameters.SegwitHeight;
   }

   public override void Dispose()
   {
      // unregister the peer from fetcher list if it was registered.
      _blockFetcherManager.UnregisterFetcher(this);

      base.Dispose();
   }
}
