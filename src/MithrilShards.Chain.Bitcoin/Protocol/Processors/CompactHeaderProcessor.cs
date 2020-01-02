using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors {
   /// <summary>
   /// Manage the Compact Header bip-0152 communication.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public class CompactHeaderProcessor : BaseProcessor {
      internal class Status {

         public VersionMessage PeerVersion { get; set; } = null;

         public bool IsHandShaked { get; set; } = false;

         public bool VersionReceived { get; set; } = false;

         public bool VersionAckReceived { get; set; } = false;
      }

      private readonly Status status = new Status();
      readonly IDateTimeProvider dateTimeProvider;
      readonly IRandomNumberGenerator randomNumberGenerator;
      readonly NodeImplementation nodeImplementation;
      readonly SelfConnectionTracker selfConnectionTracker;

      public CompactHeaderProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, NodeImplementation nodeImplementation)
         : base(logger, eventBus) {
         this.nodeImplementation = nodeImplementation;
      }

      public override async ValueTask AttachAsync(IPeerContext peerContext) {
         await base.AttachAsync(peerContext).ConfigureAwait(false);

         this.RegisterLifeTimeSubscription(this.eventBus.Subscribe<PeerHandshaked>(async (@event) => {
            await this.OnPeerHandshakedAsync(@event).ConfigureAwait(false);
         }));
      }

      private async ValueTask OnPeerHandshakedAsync(PeerHandshaked @event) {
         if (this.PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V70014) {
            await this.SendMessageAsync(new SendCmpctMessage { UseCmpctBlock = true, Version = 1 }).ConfigureAwait(false);
         }
      }

      public override async ValueTask<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation) {
         return message switch
         {
            SendCmpctMessage typedMessage => await this.ProcessSendCmpctMessageAsync(typedMessage, cancellation).ConfigureAwait(false),
            _ => true
         };
      }


      private ValueTask<bool> ProcessSendCmpctMessageAsync(SendCmpctMessage message, CancellationToken cancellation) {
         if (message.UseCmpctBlock && message.Version == 1) {
            this.AnnounceBlocksUsingCmpctBlock();
         }

         return new ValueTask<bool>(true);
      }

      private void AnnounceBlocksUsingCmpctBlock() {
         throw new NotImplementedException();
      }
   }
}
