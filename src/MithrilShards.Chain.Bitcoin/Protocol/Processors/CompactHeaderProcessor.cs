using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   /// <summary>
   /// Manage the Compact Header BIP-0152 communication.
   /// <see href="https://github.com/bitcoin/bips/blob/master/bip-0152.mediawiki"/>
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public class CompactHeaderProcessor : BaseProcessor,
      INetworkMessageHandler<SendCmpctMessage>
   {
      private class Status
      {
         public bool UseCompactHeader { get; internal set; } = false;
      }

      private readonly Status status = new Status();

      public CompactHeaderProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, NodeImplementation nodeImplementation)
         : base(logger, eventBus)
      {
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
            await this.SendMessageAsync(new SendCmpctMessage { UseCmpctBlock = true, Version = 1 }).ConfigureAwait(false);
         }
      }

      public ValueTask<bool> ProcessMessageAsync(SendCmpctMessage message, CancellationToken cancellation)
      {
         if (message.UseCmpctBlock && message.Version == 1)
         {
            this.AnnounceBlocksUsingCmpctBlock();
         }

         return new ValueTask<bool>(true);
      }

      private void AnnounceBlocksUsingCmpctBlock()
      {
         this.status.UseCompactHeader = true;
      }
   }
}