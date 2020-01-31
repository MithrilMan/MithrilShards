﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol;
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
      /// Maximum number of block hashes allowed in the BlockLocator.</summary>
      /// <seealso cref="https://lists.linuxfoundation.org/pipermail/bitcoin-dev/2018-August/016285.html"/>
      /// <seealso cref="https://github.com/bitcoin/bitcoin/pull/13907"
      /// </summary>
      private const int MAX_LOCATOR_HASHES = 101;
      readonly IDateTimeProvider dateTimeProvider;
      private readonly IChainDefinition chainDefinition;
      private readonly IInitialBlockDownloadState ibdState;
      private readonly IBlockHeaderHashCalculator blockHeaderHashCalculator;
      private readonly HeadersTree headersTree;

      public BlockHeaderProcessor(ILogger<HandshakeProcessor> logger,
                                  IEventBus eventBus,
                                  IDateTimeProvider dateTimeProvider,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IChainDefinition chainDefinition,
                                  IInitialBlockDownloadState ibdState,
                                  IBlockHeaderHashCalculator blockHeaderHashCalculator,
                                  HeadersTree headersLookup)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.dateTimeProvider = dateTimeProvider;
         this.chainDefinition = chainDefinition;
         this.ibdState = ibdState;
         this.blockHeaderHashCalculator = blockHeaderHashCalculator;
         this.headersTree = headersLookup;
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and as
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override async ValueTask OnPeerHandshakedAsync()
      {
         this.status.PeerStartingHeight = this.PeerContext.Data.Get<HandshakeProcessor.HandshakeProcessorStatus>()?.PeerVersion?.StartHeight ?? 0;

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

         if (this.ibdState.isInIBD)
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
         int protocolVersion = this.PeerContext.NegotiatedProtocolVersion.Version;
         BlockHeader[]? headers = headersMessage.Headers;
         int headersCount = headers!.Length;

         /// https://github.com/bitcoin/bitcoin/blob/b949ac9697a6cfe087f60a16c063ab9c5bf1e81f/src/net_processing.cpp#L2923-L2947
         /// bitcoin does this before deserialize the message but I don't think would be a big problem, we could ban the peer in case we find this being a vector attack.
         if (headersCount > MAX_HEADERS)
         {
            this.Misbehave(20, "Too many headers received.");
            return false;
         }
         if (headersCount == 0)
         {
            this.logger.LogDebug("Peer didn't returned any headers, let's assume we reached its tip.");
            return false;
         }

         /// If this looks like it could be a block announcement (headersCount < MAX_BLOCKS_TO_ANNOUNCE),
         /// use special logic for handling headers that don't connect:
         /// - Send a getheaders message in response to try to connect the chain.
         /// - The peer can send up to MAX_UNCONNECTING_HEADERS in a row that don't connect before giving DoS points
         /// - Once a headers message is received that is valid and does connect, unconnecting header counter gets reset back to 0.
         /// see https://github.com/bitcoin/bitcoin/blob/ceb789cf3a9075729efa07f5114ce0369d8606c3/src/net_processing.cpp#L1658-L1683
         if (!this.headersTree.IsKnown(headers[0].PreviousBlockHash) && headersCount < MAX_BLOCKS_TO_ANNOUNCE)
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

         bool newHeaderReceived = false;
         // If we don't have the last header, then they'll have given us something new (if these headers are valid).
         if (!this.headersTree.IsKnown(headers.Last().Hash))
         {
            newHeaderReceived = true;
         }

         if (!this.ProcessNewBlockHeaders(headers, out BlockValidationState state, out HeaderNode? lastHeader))
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
         }
         this.status.UnconnectingHeaderReceived = 0;

         this.UpdateBlockAvailability(lastHeader!.Hash);

         //TODO: continue from here https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1709

         if (newHeaderReceived && lastHeader.ChainWork > this.headersTree.GetTipHeaderNode().ChainWork)
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

         bool fCanDirectFetch = this.CanDirectFetch();
         // If this set of headers is valid and ends in a block with at least as much work as our tip, download as much as possible.
         if (fCanDirectFetch && lastHeader.IsValid(HeaderValidityStatuses.ValidTree) && this.headersTree.GetTipHeaderNode().ChainWork <= lastHeader.ChainWork)
         {
            continuare da net_processing L1736
            return true;
         }
      }

      private bool CanDirectFetch()
      {
         return this.headersTree.GetTip().TimeStamp > this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() - this.chainDefinition.PowTargetSpacing * 20;
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

      private bool ProcessNewBlockHeaders(BlockHeader[] headers, out BlockValidationState state, [MaybeNullWhen(false)]out HeaderNode lastHeader)
      {
         if (this.headersTree.TryAddHeaders(headers, out state, out lastHeader))
         {
            //validation.cpp L3681

            //if (NotifyHeaderTip())
            //{
            //   if (::ChainstateActive().IsInitialBlockDownload() && ppindex && *ppindex)
            //   {
            //      LogPrintf("Synchronizing blockheaders, height: %d (~%.2f%%)\n", (*ppindex)->nHeight, 100.0 / ((*ppindex)->nHeight + (GetAdjustedTime() - (*ppindex)->GetBlockTime()) / Params().GetConsensus().nPowTargetSpacing) * (*ppindex)->nHeight);
            //   }
            //}
         }
      }
   }
}