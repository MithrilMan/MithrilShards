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
         await this.SendMessageAsync(minVersion: KnownVersion.V70014, new SendCmpctMessage { UseCmpctBlock = true, Version = 1 }).ConfigureAwait(false);
         await this.SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);

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

      public ValueTask<bool> ProcessMessageAsync(SendCmpctMessage message, CancellationToken cancellation)
      {
         if (message.UseCmpctBlock && message.Version == 1)
         {
            this.status.UseCompactBlocks = true;
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

         return new ValueTask<bool>(true);
      }

      public ValueTask<bool> ProcessMessageAsync(SendHeadersMessage message, CancellationToken cancellation)
      {
         throw new System.NotImplementedException();
      }
   }
}