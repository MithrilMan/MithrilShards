using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   /// <summary>
   /// Manage the exchange of block and headers between peers.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public partial class BlockHeaderProcessor : BaseProcessor,
      INetworkMessageHandler<GetHeadersMessage>,
      INetworkMessageHandler<SendHeadersMessage>,
      INetworkMessageHandler<HeadersMessage>,
      INetworkMessageHandler<SendCmpctMessage>
   {
      /// <summary>
      /// Number of headers sent in one getheaders result.
      /// We rely on the assumption that if a peer sends less than this number, we reached its tip.
      /// Changing this value is a protocol upgrade.
      /// </summary>
      private const int MAX_HEADERS = 2000;

      /// <summary>
      /// Maximum number of headers to announce when relaying blocks with headers message.
      /// </summary>
      private const int MAX_BLOCKS_TO_ANNOUNCE = 8;

      /// <summary>
      /// Maximum number of unconnecting headers before triggering a peer Misbehave action.
      /// </summary>
      private const int MAX_UNCONNECTING_HEADERS = 10;

      /// <summary>
      /// The maximum number of blocks that can be requested from a single peer.
      /// </summary>
      private const int MAX_BLOCKS_IN_TRANSIT_PER_PEER = 26; //FIX default bitcoin value: 16

      /// <summary>
      /// Maximum number of block hashes allowed in the BlockLocator.</summary>
      /// <seealso cref="https://lists.linuxfoundation.org/pipermail/bitcoin-dev/2018-August/016285.html"/>
      /// <seealso cref="https://github.com/bitcoin/bitcoin/pull/13907"
      /// </summary>
      private const int MAX_LOCATOR_HASHES = 101;
      readonly IDateTimeProvider dateTimeProvider;
      private readonly IConsensusParameters consensusParameters;
      private readonly IInitialBlockDownloadTracker ibdState;
      private readonly IBlockHeaderHashCalculator blockHeaderHashCalculator;
      readonly IBlockDownloader blockDownloader;
      readonly ILocalServiceProvider localServiceProvider;
      readonly IConsensusValidator consensusValidator;
      private readonly HeadersTree headersTree;

      public BlockHeaderProcessor(ILogger<HandshakeProcessor> logger,
                                  IEventBus eventBus,
                                  IDateTimeProvider dateTimeProvider,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IConsensusParameters consensusParameters,
                                  IInitialBlockDownloadTracker ibdState,
                                  IBlockHeaderHashCalculator blockHeaderHashCalculator,
                                  IBlockDownloader blockDownloader,
                                  ILocalServiceProvider localServiceProvider,
                                  IConsensusValidator consensusValidator,
                                  HeadersTree headersLookup)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.dateTimeProvider = dateTimeProvider;
         this.consensusParameters = consensusParameters;
         this.ibdState = ibdState;
         this.blockHeaderHashCalculator = blockHeaderHashCalculator;
         this.blockDownloader = blockDownloader;
         this.localServiceProvider = localServiceProvider;
         this.consensusValidator = consensusValidator;
         this.headersTree = headersLookup;
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and update peer status based on its version message.
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override async ValueTask OnPeerHandshakedAsync()
      {
         VersionMessage peerVersion = this.PeerContext.Data.Get<HandshakeProcessor.HandshakeProcessorStatus>().PeerVersion!;

         this.status.PeerStartingHeight = peerVersion.StartHeight;
         this.status.CanServeWitness = (peerVersion.Services & (ulong)NodeServices.Witness) != 0;

         await this.SendMessageAsync(minVersion: KnownVersion.V70014, new SendCmpctMessage { HighBandwidthMode = true, Version = 1 }).ConfigureAwait(false);
         await this.SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);

         /// ask for blocks
         await this.SendMessageAsync(new GetHeadersMessage
         {
            Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
            BlockLocator = this.headersTree.GetTipLocator(),
            HashStop = UInt256.Zero
         }).ConfigureAwait(false);
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
         if (message is null) throw new System.ArgumentNullException(nameof(message));

         if (message.Version > 0 && message.Version <= 2)
         {
            if (message.Version > this.status.CompactVersion)
            {
               this.status.CompactVersion = message.Version;
               this.status.UseCompactBlocks = true;
               this.status.CompactBlocksHighBandwidthMode = message.HighBandwidthMode;
            }
         }
         else
         {
            this.logger.LogDebug("Ignoring sendcmpct message because its version is unknown.");
         }

         return new ValueTask<bool>(true);
      }

      public ValueTask<bool> ProcessMessageAsync(GetHeadersMessage message, CancellationToken cancellation)
      {
         if (message is null) throw new System.ArgumentNullException(nameof(message));

         if (message.BlockLocator!.BlockLocatorHashes.Length > MAX_LOCATOR_HASHES)
         {
            this.logger.LogDebug("Exceeded maximum number of block hashes for getheaders message.");
            this.Misbehave(10, "Exceeded maximum getheaders block hashes length", true);
         }

         if (this.ibdState.IsDownloadingBlocks())
         {
            this.logger.LogDebug("Ignoring getheaders from {PeerId} because node is in initial block download state.", this.PeerContext.PeerId);
            return new ValueTask<bool>(true);
         }

         HeaderNode startingNode;
         // If block locator is null, return the hashStop block
         if ((message.BlockLocator.BlockLocatorHashes?.Length ?? 0) == 0)
         {
            if (!this.headersTree.TryGetNode(message.HashStop!, true, out startingNode!))
            {
               this.logger.LogDebug("Empty block locator and HashStop not found");
               return new ValueTask<bool>(true);
            }
         }
         else
         {
            startingNode = this.headersTree.GetHighestNodeInBestChainFromBlockLocator(message.BlockLocator);
         }

         this.logger.LogDebug("Serving headers from {StartingNodeHeight}:{StartingNodeHash}", startingNode.Height, startingNode.Hash);

         return new ValueTask<bool>(true);
      }

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
            return false;
         }

         if (await this.HandleAsNotConnectingAnnouncement(headers).ConfigureAwait(false))
         {
            // fully handled as non connecting announcement
            return true;
         }

         // compute hashes in parallel to speed up the operation and check sent headers are sequential.
         Parallel.ForEach(headers, header => header.Hash = this.blockHeaderHashCalculator.ComputeHash(header, protocolVersion));

         // Ensure headers are consecutive.
         for (int i = 1; i < headersCount; i++)
         {
            if (headers[i].PreviousBlockHash != headers[i - 1].Hash)
            {
               this.Misbehave(20, "Non continuous headers sequence.");
               return false;
            }
         }

         // If we don't have the last header, then the peer gave us something new (if these headers are valid).
         bool newHeaderReceived = !this.headersTree.IsKnown(headers.Last().Hash);

         if (!this.consensusValidator.ProcessNewBlockHeaders(headers, out BlockValidationState state, out HeaderNode? lastHeader))
         {
            if (state.IsInvalid())
            {
               this.MisbehaveDuringHeaderValidation(state, "invalid header received");
               return false;
            }
         }

         if (this.status.UnconnectingHeaderReceived > 0)
         {
            this.logger.LogDebug("Resetting UnconnectingHeaderReceived, was {UnconnectingHeaderReceived}.", this.status.UnconnectingHeaderReceived);
            this.status.UnconnectingHeaderReceived = 0;
         }

         this.UpdateBlockAvailability(lastHeader!.Hash);

         if (newHeaderReceived && lastHeader.ChainWork > this.headersTree.GetTip().ChainWork)
         {
            // we received the new tip of the chain
            this.status.LastBlockAnnouncement = this.dateTimeProvider.GetTime();
         }

         if (headersCount == MAX_HEADERS)
         {
            // We received the maximum number of headers per protocol definition, the peer may have more headers.
            // TODO: optimize: if pindexLast is an ancestor of ::ChainActive().Tip or pindexBestHeader, continue
            // from there instead.
            this.logger.LogDebug("Request another getheaders from height {BlockLocatorStart} (startingHeight: {StartingHeight}).", lastHeader.Height, this.status.PeerStartingHeight);
            var newGetHeaderRequest = new GetHeadersMessage
            {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = this.headersTree.GetLocator(lastHeader.Hash),
               HashStop = UInt256.Zero
            };
            await this.SendMessageAsync(newGetHeaderRequest).ConfigureAwait(false);
         }

         // If this set of headers is valid and ends in a block with at least as much work as our tip, download as much as possible.
         if (
            this.CanDirectFetch()
            && lastHeader.IsValid(HeaderValidityStatuses.ValidTree)
            && this.headersTree.GetTip().ChainWork <= lastHeader.ChainWork
            )
         {
            List<HeaderNode> blocksToDownload = new List<HeaderNode>();
            HeaderNode? currentHeader = lastHeader;

            while (currentHeader != null && !this.headersTree.IsInBestChain(currentHeader) && blocksToDownload.Count <= MAX_BLOCKS_IN_TRANSIT_PER_PEER)
            {
               if (!currentHeader.Validity.HasFlag(HeaderValidityStatuses.HasBlockData)  // we don't have data for this block
                  && !this.blockDownloader.IsDownloading(currentHeader) // it's not already in download
                  && (!this.IsWitnessEnabled(currentHeader.Previous) || this.status.CanServeWitness) //witness isn't enabled or the other peer can't serve witness
                  )
               {
                  blocksToDownload.Add(currentHeader);
               }

               currentHeader = currentHeader.Previous;
            }

            /// If currentHeader still isn't on our main chain, we're looking at a very large reorg at a time we think we're close to caught
            /// up to the main chain -- this shouldn't really happen.
            /// Bail out on the direct fetch and rely on parallel download instead.
            if (currentHeader != null && !this.headersTree.IsInBestChain(currentHeader))
            {
               this.logger.LogDebug("Large reorg, won't direct fetch to {HeaderNode}", lastHeader);
            }
            else
            {
               var vGetData = new List<InventoryVector>();
               // Download as much as possible, from earliest to latest.
               for (int i = blocksToDownload.Count - 1; i >= 0; i--)
               {
                  HeaderNode blockToDownload = blocksToDownload[i];
                  UInt256 blockHash = blockToDownload.Hash;

                  if (this.status.BlocksInDownload >= MAX_BLOCKS_IN_TRANSIT_PER_PEER)
                  {
                     break; // Can't download any more from this peer
                  }

                  uint fetchFlags = this.GetFetchFlags();
                  vGetData.Add(new InventoryVector { Type = InventoryType.MSG_BLOCK | fetchFlags, Hash = blockHash });

                  if (this.blockDownloader.TryDownloadBlock(this.PeerContext, blockToDownload, out QueuedBlock? queuedBlock))
                  {
                     this.status.BlocksInDownload++;
                     if (this.status.BlocksInDownload == 1)
                     {
                        // We're starting a block download (batch) from this peer.
                        this.status.DownloadingSince = this.dateTimeProvider.GetTime();
                     }
                  }

                  this.logger.LogDebug("Requesting block {BlockHash}", blockHash);
               }

               if (vGetData.Count > 0)
               {
                  if (vGetData.Count > 1)
                  {
                     this.logger.LogDebug("Downloading blocks toward {HeaderNode}", lastHeader);
                  }

                  if (this.status.UseCompactBlocks
                     && this.blockDownloader.BlocksInDownload == 1
                     && lastHeader.Previous?.IsValid(HeaderValidityStatuses.ValidChain) == true)
                  {
                     // In any case, we want to download using a compact block, not a regular one
                     vGetData[0].Type = InventoryType.MSG_CMPCT_BLOCK;
                  }
                  await this.SendMessageAsync(new GetDataMessage { Inventory = vGetData.ToArray() }).ConfigureAwait(false);
               }

               if (!this.DisconnectPeerIfNotUseful(headersCount))
               {
                  // TODO maybe implement peer eviction protection
                  //if(this.IsOutboundDisconnectionCandidate() && this.status.BestKnownHeader != null)
                  //{
                  //   // If this is an outbound peer, check to see if we should protect	it from the bad/lagging chain logic.
                  //   if (g_outbound_peers_with_protect_from_disconnect < MAX_OUTBOUND_PEERS_TO_PROTECT_FROM_DISCONNECT
                  //      && this.status.BestKnownHeader.ChainWork >= this.headersTree.GetTip().ChainWork
                  //      && !this.status->m_chain_sync.m_protect)
                  //   {
                  //      this.logger.LogDebug("Protecting outbound peer from eviction");
                  //      this.status.m_chain_sync.m_protect = true;
                  //      ++g_outbound_peers_with_protect_from_disconnect;
                  //   }
                  //}
               }
            }
         }

         return true;
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
            if (this.status.BestKnownHeader != null && this.status.BestKnownHeader.ChainWork < this.consensusParameters.MinimumChainWork)
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
         if (!this.headersTree.IsKnown(headers[0].PreviousBlockHash) && headers.Length < MAX_BLOCKS_TO_ANNOUNCE)
         {
            if (++this.status.UnconnectingHeaderReceived % MAX_UNCONNECTING_HEADERS == 0)
            {
               this.Misbehave(20, "Exceeded maximum number of received unconnecting headers.");
            }

            // ask again for headers starting from current tip
            var newGetHeaderRequest = new GetHeadersMessage
            {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = this.headersTree.GetTipLocator(),
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
         return this.headersTree.GetTipAsBlockHeader().TimeStamp > this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - this.consensusParameters.PowTargetSpacing * 20;
      }

      /// <summary>
      /// Updates tracking information about which blocks a peer is assumed to have.
      /// After calling this method, status.BestKnownHeader is guaranteed to be non null.
      /// </summary>
      /// <param name="headerHash">The header hash.</param>
      private void UpdateBlockAvailability(UInt256? headerHash)
      {
         if (headerHash == null) ThrowHelper.ThrowArgumentNullException(nameof(headerHash));

         this.ProcessBlockAvailability();

         this.headersTree.TryGetNode(headerHash, false, out HeaderNode? headerNode);
         // A better block header was announced.
         if (this.status.BestKnownHeader == null || headerNode!.ChainWork >= this.status.BestKnownHeader.ChainWork)
         {
            this.status.BestKnownHeader = headerNode;
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
            if (this.headersTree.TryGetNode(this.status.LastUnknownBlockHash, false, out HeaderNode? headerNode) && headerNode.ChainWork > Target.Zero)
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