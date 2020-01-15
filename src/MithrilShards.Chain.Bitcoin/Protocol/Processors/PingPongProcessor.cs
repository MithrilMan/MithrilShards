using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public class PingPongProcessor : BaseProcessor,
      INetworkMessageHandler<PingMessage>,
      INetworkMessageHandler<PongMessage>
   {


      public PingPongProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
      }

      protected override async ValueTask OnPeerHandshakedAsync()
      {
         // ask for addresses when the peer handshakes
         await this.SendMessageAsync(minVersion: KnownVersion.V31402, new GetAddrMessage()).ConfigureAwait(false);
      }


      public async ValueTask<bool> ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
      {
         await this.SendMessageAsync(new PongMessage { Nonce = message.Nonce }).ConfigureAwait(false);

         return true;
      }

      public ValueTask<bool> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
      {
         throw new System.NotImplementedException();
      }
   }
}