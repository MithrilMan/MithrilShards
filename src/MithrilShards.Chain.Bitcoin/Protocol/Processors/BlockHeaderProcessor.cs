using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
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

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   /// <summary>
   /// Manage the exchange of block and headers between peers.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public partial class BlockHeaderProcessor : BaseProcessor, IPeriodicWorkExceptionHandler,
      INetworkMessageHandler<GetHeadersMessage>,
      INetworkMessageHandler<SendHeadersMessage>,
      INetworkMessageHandler<HeadersMessage>,
      INetworkMessageHandler<SendCmpctMessage>,
      INetworkMessageHandler<BlockMessage>
   {
      readonly IDateTimeProvider dateTimeProvider;
      private readonly IConsensusParameters consensusParameters;
      private readonly IInitialBlockDownloadTracker ibdState;
      private readonly IBlockHeaderHashCalculator blockHeaderHashCalculator;
      readonly IBlockFetcherManager blockFetcherManager;
      readonly ILocalServiceProvider localServiceProvider;
      readonly IChainState chainState;
      readonly IHeaderValidator headerValidator;
      readonly IBlockValidator blockValidator;
      readonly IPeriodicWork headerSyncLoop;
      readonly IPeriodicWork blockRequestLoop;
      readonly BitcoinSettings options;
      private Target minimumChainWork;

      public BlockHeaderProcessor(ILogger<BlockHeaderProcessor> logger,
                                  IEventBus eventBus,
                                  IDateTimeProvider dateTimeProvider,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IConsensusParameters consensusParameters,
                                  IInitialBlockDownloadTracker ibdState,
                                  IBlockHeaderHashCalculator blockHeaderHashCalculator,
                                  IBlockFetcherManager blockFetcherManager,
                                  ILocalServiceProvider localServiceProvider,
                                  IChainState chainState,
                                  IHeaderValidator headerValidator,
                                  IBlockValidator blockValidator,
                                  IPeriodicWork headerSyncLoop,
                                  IPeriodicWork blockRequestLoop,
                                  IOptions<BitcoinSettings> options)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.dateTimeProvider = dateTimeProvider;
         this.consensusParameters = consensusParameters;
         this.ibdState = ibdState;
         this.blockHeaderHashCalculator = blockHeaderHashCalculator;
         this.blockFetcherManager = blockFetcherManager;
         this.localServiceProvider = localServiceProvider;
         this.chainState = chainState;
         this.headerValidator = headerValidator;
         this.blockValidator = blockValidator;
         this.headerSyncLoop = headerSyncLoop;
         this.blockRequestLoop = blockRequestLoop;
         this.options = options.Value;


         minimumChainWork = this.options.MinimumChainWork ?? this.consensusParameters.MinimumChainWork;
         if (minimumChainWork < this.consensusParameters.MinimumChainWork)
         {
            this.logger.LogWarning($"{nameof(minimumChainWork)} set below default value of {this.consensusParameters.MinimumChainWork}");
         }

         headerSyncLoop.Configure(stopOnException: false, this);
         blockRequestLoop.Configure(stopOnException: false, this);
      }

      public void OnException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         string? disconnectionReason = failedWork switch
         {
            IPeriodicWork work when work == headerSyncLoop => "Peer header syncing loop had failures.",
            IPeriodicWork work when work == blockRequestLoop => "Peer block request loop had failures.",
            _ => null
         };

         if (disconnectionReason != null)
         {
            feedback.ContinueExecution = false;
            this.PeerContext.Disconnect(disconnectionReason);
         }
      }

      protected override ValueTask OnPeerAttachedAsync()
      {
         this.RegisterLifeTimeEventHandler<BlockHeaderValidationSucceeded>(this.OnBlockHeaderValidationSucceededAsync, arg => arg.PeerContext == PeerContext);
         this.RegisterLifeTimeEventHandler<BlockHeaderValidationFailed>(this.OnBlockHeaderValidationFailedAsync, arg => arg.PeerContext == PeerContext);

         return base.OnPeerAttachedAsync();
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and update peer status based on its version message.
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override async ValueTask OnPeerHandshakedAsync()
      {
         HandshakeProcessor.HandshakeProcessorStatus handshakeStatus = this.PeerContext.Data.Get<HandshakeProcessor.HandshakeProcessorStatus>();

         VersionMessage peerVersion = handshakeStatus.PeerVersion!;

         this.status.IsLimitedNode = this.PeerContext.IsLimitedNode;
         this.status.IsClient = this.PeerContext.IsClient;

         this.status.PeerStartingHeight = peerVersion.StartHeight;
         this.status.CanServeWitness = this.PeerContext.CanServeWitness;

         await this.SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);

         if (this.IsSupported(KnownVersion.V70014))
         {
            // Tell our peer we are willing to provide version 1 or 2 cmpctblocks.
            // However, we do not request new block announcements using cmpctblock messages.
            // We send this to non-NODE NETWORK peers as well, because they may wish to request compact blocks from us.
            if (this.localServiceProvider.HasServices(NodeServices.Witness))
            {
               await this.SendMessageAsync(new SendCmpctMessage { AnnounceUsingCompactBlock = false, Version = 2 }).ConfigureAwait(false);
            }

            await this.SendMessageAsync(new SendCmpctMessage { AnnounceUsingCompactBlock = false, Version = 1 }).ConfigureAwait(false);
         }


         // if this peer is able to serve blocks, register it
         if (!status.IsClient)
         {
            this.blockFetcherManager.RegisterFetcher(this);
         }

         // starts the header sync loop
         _ = this.headerSyncLoop.StartAsync(
               label: $"headerSyncLoop-{PeerContext.PeerId}",
               work: SyncLoopAsync,
               interval: TimeSpan.FromMilliseconds(SYNC_LOOP_INTERVAL),
               cancellation: PeerContext.ConnectionCancellationTokenSource.Token
            );

         _ = this.blockRequestLoop.StartAsync(
            label: $"{nameof(blockRequestLoop)}-{PeerContext.PeerId}",
            work: BlockRequestLoop,
            interval: TimeSpan.FromMilliseconds(BLOCK_REQUEST_INTERVAL),
            cancellation: PeerContext.ConnectionCancellationTokenSource.Token
            );
      }

      private async Task SyncLoopAsync(CancellationToken cancellationToken)
      {
         using var readLock = GlobalLocks.ReadOnMain();

         var bestHeaderNode = this.chainState.BestHeader;
         if (!this.chainState.TryGetBlockHeader(bestHeaderNode, out BlockHeader? bestBlockHeader))
         {
            ThrowHelper.ThrowNotSupportedException("BestHeader should always be available, this should never happen");
         }

         if (!this.status.IsSynchronizingHeaders)
         {
            status.IsSynchronizingHeaders = true;
            status.HeadersSyncTimeout =
               this.dateTimeProvider.GetTimeMicros()
               + HEADERS_DOWNLOAD_TIMEOUT_BASE
               + HEADERS_DOWNLOAD_TIMEOUT_PER_HEADER * (
                  (this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - bestBlockHeader.TimeStamp) / this.consensusParameters.PowTargetSpacing
                  );

            /* If possible, start at the block preceding the currently
               best known header.  This ensures that we always get a
               non-empty list of headers back as long as the peer
               is up-to-date.  With a non-empty response, we can initialise
               the peer's known best block.  This wouldn't be possible
               if we requested starting at pindexBestHeader and
               got back an empty response.  */
            var pindexStart = bestHeaderNode.Previous ?? bestHeaderNode;

            this.logger.LogDebug("Starting syncing headers from height {LocatorHeight} (peer startheight: {startheight})", pindexStart.Height, this.status.PeerStartingHeight);

            var newGetHeaderRequest = new GetHeadersMessage
            {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = this.chainState.GetLocator(pindexStart),
               HashStop = UInt256.Zero
            };

            await this.SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);
         }

         this.CheckSyncStallingLocked(bestBlockHeader);

         this.ConsiderEviction(this.dateTimeProvider.GetTime());
      }

      private void CheckSyncStallingLocked(BlockHeader bestHeader)
      {
         // Check for headers sync timeouts
         if (status.IsSynchronizingHeaders && status.HeadersSyncTimeout < long.MaxValue)
         {
            var now = this.dateTimeProvider.GetTimeMicros();
            // Detect whether this is a stalling initial-headers-sync peer
            if (bestHeader.TimeStamp <= this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - 24 * 60 * 60)
            {
               bool isTheOnlyPeerSynching = true; //nSyncStarted == 1 && (nPreferredDownload - state.fPreferredDownload >= 1)
               if (now > status.HeadersSyncTimeout && isTheOnlyPeerSynching)
               {
                  // Disconnect a (non-whitelisted) peer if it is our only sync peer,
                  // and we have others we could be using instead.
                  // Note: If all our peers are inbound, then we won't
                  // disconnect our sync peer for stalling; we have bigger
                  // problems if we can't get any outbound peers.
                  if (!this.PeerContext.Permissions.Has(BitcoinPeerPermissions.NOBAN))
                  {
                     this.PeerContext.Disconnect("Timeout downloading headers, disconnecting");
                     return;
                  }
                  else
                  {
                     this.logger.LogDebug("Timeout downloading headers from whitelisted peer {PeerId}, not disconnecting.", this.PeerContext.PeerId);
                     // Reset the headers sync state so that we have a
                     // chance to try downloading from a different peer.
                     // Note: this will also result in at least one more
                     // getheaders message to be sent to
                     // this peer (eventually).
                     status.IsSynchronizingHeaders = false;
                     status.HeadersSyncTimeout = 0;
                  }
               }
            }
            else
            {
               // After we've caught up once, reset the timeout so we can't trigger disconnect later.
               status.HeadersSyncTimeout = long.MaxValue;
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
      /// The other peer prefer to be announced about new block using headers
      /// </summary>
      public ValueTask<bool> ProcessMessageAsync(SendHeadersMessage message, CancellationToken cancellation)
      {
         this.status.AnnounceNewBlockUsingSendHeaders = true;
         return new ValueTask<bool>(true);
      }

      /// <summary>
      /// The other peer prefer to receive blocks using cmpct messages.
      /// </summary>
      public ValueTask<bool> ProcessMessageAsync(SendCmpctMessage message, CancellationToken cancellation)
      {
         if (message.Version == 1 || (this.localServiceProvider.HasServices(NodeServices.Witness) && message.Version == 2))
         {
            if (!this.status.ProvidesHeaderAndIDs)
            {
               this.status.ProvidesHeaderAndIDs = true;
               this.status.WantsCompactWitness = message.Version == 2;
            }

            // ignore later version announces
            if (this.status.WantsCompactWitness = (message.Version == 2))
            {
               this.status.AnnounceUsingCompactBlock = message.AnnounceUsingCompactBlock;
            }

            if (!this.status.SupportsDesiredCompactVersion)
            {
               if (this.localServiceProvider.HasServices(NodeServices.Witness))
               {
                  this.status.SupportsDesiredCompactVersion = (message.Version == 2);
               }
               else
               {
                  this.status.SupportsDesiredCompactVersion = (message.Version == 1);
               }
            }
         }
         else
         {
            this.logger.LogDebug("Ignoring sendcmpct message because its version is unknown.");
         }

         return new ValueTask<bool>(true);
      }

      /// <summary>
      /// The peer wants some headers from us
      /// </summary>
      public async ValueTask<bool> ProcessMessageAsync(GetHeadersMessage message, CancellationToken cancellation)
      {
         if (message is null) ThrowHelper.ThrowArgumentException(nameof(message));

         if (message.BlockLocator!.BlockLocatorHashes.Length > MAX_LOCATOR_SIZE)
         {
            this.logger.LogDebug("Exceeded maximum block locator size for getheaders message.");
            this.Misbehave(10, "Exceeded maximum getheaders block locator size", true);
            return true;
         }

         if (this.ibdState.IsDownloadingBlocks())
         {
            this.logger.LogDebug("Ignoring getheaders from {PeerId} because node is in initial block download state.", this.PeerContext.PeerId);
            return true;
         }

         HeaderNode? startingNode;
         // If block locator is null, return the hashStop block
         if ((message.BlockLocator.BlockLocatorHashes?.Length ?? 0) == 0)
         {
            if (!this.chainState.TryGetBestChainHeaderNode(message.HashStop!, out startingNode!))
            {
               this.logger.LogDebug("Empty block locator and HashStop not found");
               return true;
            }

            //TODO (ref net_processing.cpp 2479 tag 0.20)
            //if (!BlockRequestAllowed(pindex, chainparams.GetConsensus()))
            //{
            //   LogPrint(BCLog::NET, "%s: ignoring request from peer=%i for old block header that isn't in the main chain\n", __func__, pfrom->GetId());
            //   return true;
            //}
         }
         else
         {
            // Find the last block the caller has in the main chain
            startingNode = this.chainState.FindForkInGlobalIndex(message.BlockLocator);
            this.chainState.TryGetNext(startingNode, out startingNode);
         }

         this.logger.LogDebug("Serving headers from {StartingNodeHeight}:{StartingNodeHash}", startingNode?.Height, startingNode?.Hash);

         List<BlockHeader> headersToSend = new List<BlockHeader>();
         HeaderNode? headerToSend = startingNode;
         while (headerToSend != null)
         {
            if (!this.chainState.TryGetBlockHeader(headerToSend, out BlockHeader? blockHeader))
            {
               //fatal error, should never happen
               ThrowHelper.ThrowNotSupportedException("Block Header not found");
               return true;
            }
            headersToSend.Add(blockHeader);
         }

         await this.SendMessageAsync(new HeadersMessage
         {
            Headers = headersToSend.ToArray()
         }).ConfigureAwait(false);

         return true;
      }

      /// <summary>
      /// The peer sent us headers.
      /// </summary>
      public async ValueTask<bool> ProcessMessageAsync(HeadersMessage headersMessage, CancellationToken cancellation)
      {
         BlockHeader[]? headers = headersMessage.Headers;
         int headersCount = headers!.Length;

         /// https://github.com/bitcoin/bitcoin/blob/b949ac9697a6cfe087f60a16c063ab9c5bf1e81f/src/net_processing.cpp#L2923-L2947
         /// bitcoin does this before deserialize the message but I don't think would be a big problem, we could ban the peer in case we find this being a vector attack.
         if (headersCount > MAX_HEADERS)
         {
            this.Misbehave(20, "Too many headers received.");
            return false;
         }

         //// Ignore headers received while importing
         //if (fImporting || fReindex)
         //{
         //   this.logger.LogDebug("Unexpected headers message received from peer {RemoteEndPoint}", this.PeerContext.RemoteEndPoint);
         //   return false;
         //}

         return await this.ProcessHeaders(headers).ConfigureAwait(false);
      }

      /// <summary>
      /// Processes the headers.
      /// It's invoked because of "headers" or "cmpctblock" message
      /// </summary>
      /// <param name="headers">The headers.</param>
      /// <returns></returns>
      private async Task<bool> ProcessHeaders(BlockHeader[] headers)
      {
         int protocolVersion = this.PeerContext.NegotiatedProtocolVersion.Version;
         int headersCount = headers.Length;

         if (headersCount == 0)
         {
            this.logger.LogDebug("Peer didn't returned any headers, let's assume we reached its tip.");
            return true;
         }

         using (var readLock = GlobalLocks.ReadOnMain())
         {
            if (await this.HandleAsNotConnectingAnnouncement(headers).ConfigureAwait(false))
            {
               // fully handled as non connecting announcement
               return true;
            }

            // compute hashes in parallel to speed up the operation and check sent headers are sequential.
            Parallel.ForEach(headers, header =>
            {
               header.Hash = this.blockHeaderHashCalculator.ComputeHash(header, protocolVersion);
            });
         }

         // Ensure headers are consecutive.
         for (int i = 1; i < headersCount; i++)
         {
            if (headers[i].PreviousBlockHash != headers[i - 1].Hash)
            {
               this.Misbehave(20, "Non continuous headers sequence.");
               return false;
            }
         }

         //enqueue headers for validation
         await this.headerValidator.RequestValidationAsync(new HeadersToValidate(headers, PeerContext)).ConfigureAwait(false);

         return true;
      }

      /// <summary>
      /// The node received a block.
      /// </summary>
      public ValueTask<bool> ProcessMessageAsync(BlockMessage message, CancellationToken cancellation)
      {
         BlockHeader header = message.Block!.Header!;
         bool forceProcessing = false;
         header.Hash = this.blockHeaderHashCalculator.ComputeHash(header, this.PeerContext.NegotiatedProtocolVersion.Version);

         this.eventBus.Publish(new BlockReceived(message.Block!, this.PeerContext, this));

         bool fNewBlock = false;

         //enqueue headers for validation
         await this.blockValidator.RequestValidationAsync(new BlockToValidate(message.Block!, PeerContext)).ConfigureAwait(false);

         //chainman.ProcessNewBlock(chainparams, pblock, forceProcessing, &fNewBlock);
         //if (fNewBlock)
         //{
         //   pfrom.nLastBlockTime = GetTime();
         //}
         //else
         //{
         //   LOCK(cs_main);
         //   mapBlockSource.erase(pblock->GetHash());
         //}
         return new ValueTask<bool>(true);
      }

      /// <summary>
      /// Called when block header validation failed.
      /// </summary>
      /// <param name="arg">The argument.</param>
      /// <returns></returns>
      private ValueTask OnBlockHeaderValidationFailedAsync(BlockHeaderValidationFailed arg)
      {
         this.logger.LogDebug("Header Validation failed");
         this.MisbehaveDuringHeaderValidation(arg.ValidationState, "invalid header received");

         return default;
      }

      /// <summary>
      /// Called when block header validation succeeded.
      /// </summary>
      /// <param name="arg">The argument.</param>
      /// <returns></returns>
      private async ValueTask OnBlockHeaderValidationSucceededAsync(BlockHeaderValidationSucceeded arg)
      {
         this.logger.LogDebug("Header Validation succeeded");
         var lastValidatedHeaderNode = arg.LastValidatedHeaderNode;

         using (var readLock = GlobalLocks.ReadOnMain())
         {
            if (this.status.UnconnectingHeaderReceived > 0)
            {
               this.logger.LogDebug("Resetting UnconnectingHeaderReceived, was {UnconnectingHeaderReceived}.", this.status.UnconnectingHeaderReceived);
               this.status.UnconnectingHeaderReceived = 0;
            }

            this.UpdateBlockAvailability(lastValidatedHeaderNode!.Hash);

            if (arg.NewHeadersFoundCount > 0 && lastValidatedHeaderNode.ChainWork > this.chainState.GetTip().ChainWork)
            {
               // we received the new tip of the chain
               this.status.LastBlockAnnouncement = this.dateTimeProvider.GetTime();
            }

            if (arg.ValidatedHeadersCount == MAX_HEADERS)
            {
               // We received the maximum number of headers per protocol definition, the peer may have more headers.
               // TODO: optimize: if pindexLast is an ancestor of ::ChainActive().Tip or pindexBestHeader, continue
               // from there instead.
               this.logger.LogDebug("Request another getheaders from height {BlockLocatorStart} (startingHeight: {StartingHeight}).", lastValidatedHeaderNode.Height, this.status.PeerStartingHeight);
               var newGetHeaderRequest = new GetHeadersMessage
               {
                  Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
                  BlockLocator = this.chainState.GetLocator(lastValidatedHeaderNode),
                  HashStop = UInt256.Zero
               };
               await this.SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);
            }

            this.DisconnectPeerIfNotUseful(arg.ValidatedHeadersCount);
         }
      }

      private bool ShouldRequestCompactBlock(HeaderNode lastHeader)
      {
         return this.status.SupportsDesiredCompactVersion
            //TODO fix     && this.blockFetcherManager.BlocksInDownload == 0
            && lastHeader.Previous?.IsValid(HeaderValidityStatuses.ValidChain) == true;
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
         if (this.ibdState.IsDownloadingBlocks() && headersCount != MAX_HEADERS)
         {
            // When nCount < MAX_HEADERS_RESULTS, we know we have no more headers to fetch from this peer.
            if (this.status.BestKnownHeader != null && this.status.BestKnownHeader.ChainWork < minimumChainWork)
            {
               /// This peer has too little work on their headers chain to help us sync so disconnect if it's using an outbound
               /// slot, unless the peer is whitelisted or addnode.
               /// Note: We compare their tip to nMinimumChainWork (rather than current chain tip) because we won't start block
               /// download until we have a headers chain that has at least nMinimumChainWork, even if a peer has a chain past
               /// our tip, as an anti-DoS measure.
               if (this.IsOutboundDisconnectionCandidate())
               {
                  this.PeerContext.Disconnect("Outbound peer headers chain has insufficient work.");
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
         return this.PeerContext.Direction == PeerConnectionDirection.Outbound;
      }

      /// <summary>
      /// If this looks like it could be a block announcement (headersCount < MAX_BLOCKS_TO_ANNOUNCE),
      /// use special logic for handling headers that don't connect:
      /// - Send a getheaders message in response to try to connect the chain.
      /// - The peer can send up to MAX_UNCONNECTING_HEADERS in a row that don't connect before giving DoS points
      /// - Once a headers message is received that is valid and does connect, unconnecting header counter gets reset back to 0.
      /// see https://github.com/bitcoin/bitcoin/blob/ceb789cf3a9075729efa07f5114ce0369d8606c3/src/net_processing.cpp#L1658-L1683
      /// </summary>
      /// <returns><see langword="true"/> if it has been fully handled like a block announcement.</returns>
      private async Task<bool> HandleAsNotConnectingAnnouncement(BlockHeader[] headers)
      {
         if (!this.chainState.TryGetKnownHeaderNode(headers[0].PreviousBlockHash, out _) && headers.Length < MAX_BLOCKS_TO_ANNOUNCE)
         {
            this.status.UnconnectingHeaderReceived++;
            if (this.status.UnconnectingHeaderReceived % MAX_UNCONNECTING_HEADERS == 0)
            {
               this.Misbehave(20, "Exceeded maximum number of received unconnecting headers.");
            }

            // ask again for headers starting from current tip
            var newGetHeaderRequest = new GetHeadersMessage
            {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = this.chainState.GetTipLocator(),
               HashStop = UInt256.Zero
            };
            await this.SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);

            this.logger.LogDebug("received an unconnecting header, missing {PrevBlock}. Request again headers from {BlockLocator}",
                                 headers[0].PreviousBlockHash,
                                 newGetHeaderRequest.BlockLocator.BlockLocatorHashes[0]);

            this.UpdateBlockAvailability(headers[^1].Hash);
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
         if (this.localServiceProvider.HasServices(NodeServices.Witness) && this.status.CanServeWitness)
         {
            nFetchFlags |= InventoryType.MSG_WITNESS_FLAG;
         }

         return nFetchFlags;
      }

      private bool CanDirectFetch()
      {
         return this.chainState.GetTipHeader().TimeStamp > this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - this.consensusParameters.PowTargetSpacing * 20;
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

         this.ProcessBlockAvailability();

         if (this.chainState.TryGetKnownHeaderNode(headerHash, out HeaderNode? headerNode) && headerNode.ChainWork > Target.Zero)
         {
            // A better block header was announced.
            if (this.status.BestKnownHeader == null || headerNode.ChainWork >= this.status.BestKnownHeader.ChainWork)
            {
               this.status.BestKnownHeader = headerNode;
            }
         }
         else
         {
            // An unknown block header was announced, assuming it's the best one.
            this.status.LastUnknownBlockHash = headerHash;
         }
      }

      /// <summary>
      /// Check whether the last unknown block header a peer advertised is finally known.
      /// </summary>
      /// <remarks>
      /// If <see cref="status.LastUnknownBlockHash"/> is finally found in the headers tree, it means
      /// it's no longer unknown and we set to null the status property.
      /// </remarks>
      private void ProcessBlockAvailability()
      {
         if (this.status.LastUnknownBlockHash != null)
         {
            if (this.chainState.TryGetKnownHeaderNode(this.status.LastUnknownBlockHash, out HeaderNode? headerNode) && headerNode.ChainWork > Target.Zero)
            {
               if (this.status.BestKnownHeader == null || headerNode.ChainWork >= this.status.BestKnownHeader.ChainWork)
               {
                  this.status.BestKnownHeader = headerNode;
               }
               this.status.LastUnknownBlockHash = null;
            }
         }
      }

      private bool IsWitnessEnabled(HeaderNode? headerNode)
      {
         return (headerNode?.Height ?? 0) + 1 >= this.consensusParameters.SegwitHeight;
      }
   }
}