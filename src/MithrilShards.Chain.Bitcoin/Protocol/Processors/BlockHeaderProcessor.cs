﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
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
      private const int MAX_HEADERS = 2000;

      private readonly IChainDefinition chainDefinition;

      public BlockHeaderProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager, IChainDefinition chainDefinition)
         : base(logger, eventBus, peerBehaviorManager)
      {
         this.chainDefinition = chainDefinition;
      }

      public override async ValueTask AttachAsync(IPeerContext peerContext)
      {
         await base.AttachAsync(peerContext).ConfigureAwait(false);

         this.RegisterLifeTimeSubscription(this.eventBus.Subscribe<PeerHandshaked>(async (@event) =>
         {
            await this.OnPeerHandshakedAsync(@event).ConfigureAwait(false);
         }));
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and as
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      private async ValueTask OnPeerHandshakedAsync(PeerHandshaked @event)
      {
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
         // TODO: give back our headers
         return new ValueTask<bool>(true);
      }

      public ValueTask<bool> ProcessMessageAsync(HeadersMessage headers, CancellationToken cancellation)
      {
         //https://github.com/bitcoin/bitcoin/blob/b949ac9697a6cfe087f60a16c063ab9c5bf1e81f/src/net_processing.cpp#L2923-L2947
         if (headers.Headers?.Length > MAX_HEADERS)
         {
            this.peerBehaviorManager.Misbehave(this.PeerContext, 20, "Too many headers received.");
            return new ValueTask<bool>(false);
         }

         //NEED TO COMPUTE BLOCK HASH

         ////In the special case where the remote node is at height 0 as well as us, then the headers count will be 0
         //if (headers.Headers.Length == 0 && this.PeerContext. PeerVersion.StartHeight == 0 && currentTip.Hash == this.chainDefinition.Genesis)
         //   return;
         //if (headers.Headers.Length == 1 && headers.Headers[0].ha GetHash() == currentTip.Hash)
         //   return;
         //foreach (var header in headers.Headers)
         //{
         //   var h = header.GetHash();
         //   if (h == currentTip.Hash)
         //      continue;

         //   if (header.HashPrevBlock == currentTip.Hash)
         //   {
         //      isOurs = true;
         //      currentTip = new SlimChainedBlock(h, currentTip.Hash, currentTip.Height + 1);
         //      chain.TrySetTip(currentTip.Hash, currentTip.Previous);
         //      if (currentTip.Hash == hashStop)
         //         return;
         //   }
         //   else if (chain.TrySetTip(h, header.HashPrevBlock))
         //   {
         //      currentTip = chain.TipBlock;
         //   }
         //   else
         //      break;
         //}

         return new ValueTask<bool>(true);
      }
   }
}