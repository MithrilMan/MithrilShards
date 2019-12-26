using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors {
   public class HandshakeProcessor : BaseProcessor {
      internal class Status {

         public VersionMessage PeerVersion { get; set; } = null;

         public bool IsHandShaked { get; set; } = false;

         public bool VersionReceived { get; set; } = false;
      }

      private readonly Status status = new Status();
      readonly IDateTimeProvider dateTimeProvider;
      readonly IRandomNumberGenerator randomNumberGenerator;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus, IDateTimeProvider dateTimeProvider, IRandomNumberGenerator randomNumberGenerator) : base(logger, eventBus) {
         this.PeerContext.Data.Set(this.status);
         this.dateTimeProvider = dateTimeProvider;
         this.randomNumberGenerator = randomNumberGenerator;
      }

      public override async Task<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation) {
         switch (message) {
            case VersionMessage typedMessage:
               return await this.ProcessVersionMessageAsync(typedMessage, cancellation).ConfigureAwait(false);
         }

         return true;
      }


      private async Task<bool> ProcessVersionMessageAsync(VersionMessage version, CancellationToken cancellation) {
         if (!this.status.VersionReceived && this.PeerContext.Direction == PeerConnectionDirection.Inbound) {
            await this.StartHandshakeAsync(version, cancellation).ConfigureAwait(false);
         }
         else {
            NegotiatedVersion negotiatedVersion = this.PeerContext.Data.Get<NegotiatedVersion>();

            if (negotiatedVersion.Version >= KnownVersion.V70002) {
               //var rejectPayload = new RejectPayload() {
               //   Code = RejectCode.DUPLICATE
               //};

               //await this.SendMessageAsync(rejectPayload, cancellation).ConfigureAwait(false);
               this.logger.LogWarning("Rejecting {MessageType}. ", nameof(VersionMessage));
            }
         }

         this.PeerContext.TimeOffset = this.dateTimeProvider.GetTimeOffset() - version.Timestamp;
         if ((version.Services & (ulong)NodeServices.NODE_WITNESS) != 0) {
            //TODO
            // this.SupportedTransactionOptions |= TransactionOptions.Witness;
         }

         return true;
      }

      private async Task StartHandshakeAsync(VersionMessage version, CancellationToken cancellation) {
         await this.messageWriter.WriteAsync(this.CreateVersionMessage()).ConfigureAwait(false);
         //TODO
      }

      public void Dispose() {
      }


      private VersionMessage CreateVersionMessage() {
         var version = new VersionMessage() {
            Nonce = this.randomNumberGenerator.GetUint64(),
            UserAgent = "MithrilShards.Forge",
            Version = KnownVersion.CurrentVersion,
            Timestamp = this.dateTimeProvider.GetTimeOffset(),
            ReceiverAddress = new Serialization.Types.NetworkAddress(true) {
               EndPoint = this.PeerContext.RemoteEndPoint,
            },
            SenderAddress = new Serialization.Types.NetworkAddress(true) {
               EndPoint = this.PeerContext.PublicEndPoint,
            },
            Relay = true, //this.IsRelay, TODO: it's part of the node settings
            Services = (ulong)NodeServices.Network // TODO: it's part of the node settings and depends on the configured features/shards
         };

         return version;
      }
   }
}
