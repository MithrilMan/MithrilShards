using System;
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

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors {
   /// <summary>
   /// Manage the Compact Header BIP-0152 communication.
   /// <see href="https://github.com/bitcoin/bips/blob/master/bip-0152.mediawiki"/>
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public class CompactHeaderProcessor : BaseProcessor {
      internal class Status {

         public bool UseCompactHeader { get; set; } = false;
      }

      private readonly Status status = new Status();
      private readonly IChainDefinition chainDefinition;
      readonly NodeImplementation nodeImplementation;

      public CompactHeaderProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, IChainDefinition chainDefinition, NodeImplementation nodeImplementation)
         : base(logger, eventBus) {
         this.chainDefinition = chainDefinition;
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

            /// ask for blocks
            /// TODO: BloclLocator creation have to be demanded to a BlockLocatorProvider
            /// TODO: This logic should be moved probably elsewhere because it's not BIP-0152 related
            await this.SendMessageAsync(new GetHeadersMessage {
               Version = (uint)this.PeerContext.NegotiatedProtocolVersion.Version,
               BlockLocator = new Serialization.Types.BlockLocator {
                  BlockLocatorHashes = new UInt256[1] { this.chainDefinition.Genesis }
               },
               HashStop = UInt256.Zero
            }).ConfigureAwait(false);
         }
      }

      public override async ValueTask<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation) {
         return message switch
         {
            SendCmpctMessage sendCmpct => await this.ProcessSendCmpctMessageAsync(sendCmpct, cancellation).ConfigureAwait(false),
            GetHeadersMessage getHeaders => await this.GetHeadersMessageAsync(getHeaders, cancellation).ConfigureAwait(false),
            HeadersMessage headers => await this.HeadersMessageAsync(headers, cancellation).ConfigureAwait(false),
            _ => true
         };
      }

      private ValueTask<bool> GetHeadersMessageAsync(GetHeadersMessage message, CancellationToken cancellation) {
         return new ValueTask<bool>(true);
      }

      private ValueTask<bool> HeadersMessageAsync(HeadersMessage message, CancellationToken cancellation) {
         return new ValueTask<bool>(true);
      }

      private ValueTask<bool> ProcessSendCmpctMessageAsync(SendCmpctMessage message, CancellationToken cancellation) {
         if (message.UseCmpctBlock && message.Version == 1) {
            this.AnnounceBlocksUsingCmpctBlock();
         }

         return new ValueTask<bool>(true);
      }

      private void AnnounceBlocksUsingCmpctBlock() {
         this.status.UseCompactHeader = true;
      }
   }
}