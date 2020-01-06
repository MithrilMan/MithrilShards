using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
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
   /// Manage the exchange of block headers between peers.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public class HeaderProcessor : BaseProcessor,
      INetworkMessageHandler<GetHeadersMessage>,
      INetworkMessageHandler<HeadersMessage>
   {
      private const int MAX_HEADERS = 2000;

      private readonly IChainDefinition chainDefinition;

      public HeaderProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager, IChainDefinition chainDefinition)
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

      private async ValueTask OnPeerHandshakedAsync(PeerHandshaked @event)
      {
         if (this.PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V70014)
         {
            /// ask for blocks
            /// TODO: BloclLocator creation have to be demanded to a BlockLocatorProvider
            /// TODO: This logic should be moved probably elsewhere because it's not BIP-0152 related
            await this.SendMessageAsync(new GetHeadersMessage
            {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = new Serialization.Types.BlockLocator
               {
                  BlockLocatorHashes = new UInt256[1] { this.chainDefinition.Genesis }
               },
               HashStop = UInt256.Zero
            }).ConfigureAwait(false);
         }
      }

      public ValueTask<bool> ProcessMessageAsync(GetHeadersMessage message, CancellationToken cancellation)
      {
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

         return new ValueTask<bool>(true);
      }
   }
}