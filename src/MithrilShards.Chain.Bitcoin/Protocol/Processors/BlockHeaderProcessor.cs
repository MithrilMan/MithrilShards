using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
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

      private readonly IChainDefinition chainDefinition;
      private readonly IInitialBlockDownloadState ibdState;
      private readonly IBlockHeaderHashCalculator blockHeaderHashCalculator;
      private readonly HeadersTree headersLookup;

      public BlockHeaderProcessor(ILogger<HandshakeProcessor> logger,
                                  IEventBus eventBus,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IChainDefinition chainDefinition,
                                  IInitialBlockDownloadState ibdState,
                                  IBlockHeaderHashCalculator blockHeaderHashCalculator,
                                  HeadersTree headersLookup)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.chainDefinition = chainDefinition;
         this.ibdState = ibdState;
         this.blockHeaderHashCalculator = blockHeaderHashCalculator;
         this.headersLookup = headersLookup;
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and as
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override async ValueTask OnPeerHandshakedAsync()
      {
         this.status.PeerStartingHeight = this.PeerContext.Data.Get<HandshakeProcessor.Status>().PeerVersion.StartHeight;

         await this.SendMessageAsync(minVersion: KnownVersion.V70014, new SendCmpctMessage { HighBandwidthMode = true, Version = 1 }).ConfigureAwait(false);
         await this.SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);

         /// ask for blocks
         /// TODO: BloclLocator creation have to be demanded to a BlockLocatorProvider
         /// TODO: This logic should be moved probably elsewhere because it's not BIP-0152 related
         await this.SendMessageAsync(new GetHeadersMessage
         {
            Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
            BlockLocator = new Types.BlockLocator
            {
               BlockLocatorHashes = new UInt256[1] { this.chainDefinition.Genesis }
            },
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
         if (message.BlockLocator.BlockLocatorHashes.Length > MAX_LOCATOR_HASHES)
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
            if (!this.headersLookup.TryGetNode(message.HashStop, true, out startingNode))
            {
               this.logger.LogDebug("Empty block locator and HashStop not found");
               return new ValueTask<bool>(true);
            }
         }
         else
         {
            startingNode = this.headersLookup.GetHighestNodeInBestChain(message.BlockLocator);
         }

         this.logger.LogDebug("Serving headers from {StartingNodeHeight}:{StartingNodeHash}", startingNode.Height, startingNode.Hash);

         return new ValueTask<bool>(true);
      }

      public ValueTask<bool> ProcessMessageAsync(HeadersMessage headers, CancellationToken cancellation)
      {
         int headersCount = headers.Headers.Length;

         /// https://github.com/bitcoin/bitcoin/blob/b949ac9697a6cfe087f60a16c063ab9c5bf1e81f/src/net_processing.cpp#L2923-L2947
         /// bitcoin does this before deserialize the message but I don't think would be a big problem, we could ban the peer in case we find this being a vector attack.
         if (headersCount > MAX_HEADERS)
         {
            this.Misbehave(20, "Too many headers received.");
            return new ValueTask<bool>(false);
         }

         ///// If this looks like it could be a block announcement (headersCount < MAX_BLOCKS_TO_ANNOUNCE),
         ///// use special logic for handling headers that don't connect:
         ///// - Send a getheaders message in response to try to connect the chain.
         ///// - The peer can send up to MAX_UNCONNECTING_HEADERS in a row that don't connect before giving DoS points
         ///// - Once a headers message is received that is valid and does connect, nUnconnectingHeaders gets reset back to 0.
         ///// see https://github.com/bitcoin/bitcoin/blob/ceb789cf3a9075729efa07f5114ce0369d8606c3/src/net_processing.cpp#L1658-L1683
         //if (!LookupBlockIndex(headers[0].hashPrevBlock) && nCount < MAX_BLOCKS_TO_ANNOUNCE)
         //{
         //   nodestate->nUnconnectingHeaders++;
         //   connman->PushMessage(pfrom, msgMaker.Make(NetMsgType::GETHEADERS, ::ChainActive().GetLocator(pindexBestHeader), uint256()));
         //   LogPrint(BCLog::NET, "received header %s: missing prev block %s, sending getheaders (%d) to end (peer=%d, nUnconnectingHeaders=%d)\n",
         //           headers[0].GetHash().ToString(),
         //           headers[0].hashPrevBlock.ToString(),
         //           pindexBestHeader->nHeight,
         //           pfrom->GetId(), nodestate->nUnconnectingHeaders);
         //   // Set hashLastUnknownBlock for this peer, so that if we
         //   // eventually get the headers - even from a different peer -
         //   // we can use this peer to download.
         //   UpdateBlockAvailability(pfrom->GetId(), headers.back().GetHash());

         //   if (nodestate->nUnconnectingHeaders % MAX_UNCONNECTING_HEADERS == 0)
         //   {
         //      Misbehaving(pfrom->GetId(), 20);
         //   }
         //   return true;
         //}


         //In the special case where the remote node is at height 0 as well as us, then the headers count will be 0
         if (headers.Headers.Length == 0 && this.status.PeerStartingHeight == 0 && this.headersLookup.Tip == this.chainDefinition.Genesis)
            return new ValueTask<bool>(true);

         //HeaderNode currentTip = this.headersLookup.GetTipHeaderNode();

         int protocolVersion = this.PeerContext.NegotiatedProtocolVersion.Version;
         foreach (Types.BlockHeader header in headers.Headers)
         {
            UInt256 computedHash = this.blockHeaderHashCalculator.ComputeHash(header, protocolVersion);


            switch (this.headersLookup.TrySetTip(computedHash, header.PreviousBlockHash))
            {
               case ConnectHeaderResult.Connected:
               case ConnectHeaderResult.SameTip:
               case ConnectHeaderResult.Rewinded:
               case ConnectHeaderResult.ResettedToGenesis:
                  //currentTip = currentTip.BuildNext(computedHash);
                  break;
               case ConnectHeaderResult.MissingPreviousHeader:
                  // todo gestire il resync
                  return new ValueTask<bool>(true);
            }
         }
         return new ValueTask<bool>(true);
      }
   }
}