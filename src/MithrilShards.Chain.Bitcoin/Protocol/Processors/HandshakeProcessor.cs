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
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors {
   public class HandshakeProcessor : BaseProcessor {
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

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IDateTimeProvider dateTimeProvider,
                                IRandomNumberGenerator randomNumberGenerator,
                                NodeImplementation nodeImplementation,
                                SelfConnectionTracker selfConnectionTracker) : base(logger, eventBus) {
         this.dateTimeProvider = dateTimeProvider;
         this.randomNumberGenerator = randomNumberGenerator;
         this.nodeImplementation = nodeImplementation;
         this.selfConnectionTracker = selfConnectionTracker;
      }

      public override async ValueTask AttachAsync(IPeerContext peerContext) {
         await base.AttachAsync(peerContext).ConfigureAwait(false);

         // ensures the handshake is performed timely
         this.DisconnectIfAsync(() => {
            return this.status.IsHandShaked == false;
         }, TimeSpan.FromSeconds(5), "Handshake not performed in time");//this.PeerContext.Disconnected);

         if (peerContext.Direction == PeerConnectionDirection.Outbound) {
            this.logger.LogDebug("Commencing handshake with local Version.");
            await this.messageWriter.WriteAsync(this.CreateVersionMessage()).ConfigureAwait(false);
         }
      }


      public override async Task<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation) {
         switch (message) {
            case VersionMessage typedMessage:
               await this.ProcessVersionMessageAsync(typedMessage, cancellation).ConfigureAwait(false);
               return false;
            case VerackMessage typedMessage:
               await this.ProcessVerackMessageAsync(typedMessage, cancellation).ConfigureAwait(false);
               return false;
         }

         return true;
      }


      private async Task ProcessVersionMessageAsync(VersionMessage version, CancellationToken cancellation) {
         if (this.status.VersionReceived) {
            if (this.status.IsHandShaked) {
               this.logger.LogDebug("Receiving version while already handshaked, disconnect.");
               throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
            }
            if (this.PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V70002) {
               var rejectMessage = new RejectMessage() {
                  Code = RejectMessage.RejectCode.Duplicate
               };

               await this.messageWriter.WriteAsync(rejectMessage, cancellation).ConfigureAwait(false);
               this.logger.LogWarning("Rejecting {MessageType}.", nameof(VersionMessage));
            }
            //TODO bad behavior, call peer score manager
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
         }
         else {
            this.status.VersionReceived = true;
            this.status.PeerVersion = version;

            if (this.status.VersionAckReceived) {
               await this.OnHandshaked().ConfigureAwait(false);
            }
            else {
               this.logger.LogDebug("Waiting verack...");
               if (this.PeerContext.Direction == PeerConnectionDirection.Inbound) {
                  await this.StartHandshakeAsync(cancellation).ConfigureAwait(false);
               }
               else {
                  await this.messageWriter.WriteAsync(new VerackMessage()).ConfigureAwait(false);
               }
            }
         }

         this.PeerContext.TimeOffset = this.dateTimeProvider.GetTimeOffset() - version.Timestamp;
         if ((version.Services & (ulong)NodeServices.Witness) != 0) {
            //TODO
            // this.SupportedTransactionOptions |= TransactionOptions.Witness;
         }
      }

      private async Task ProcessVerackMessageAsync(VerackMessage verack, CancellationToken cancellation) {
         if (this.status.VersionAckReceived) {
            this.logger.LogDebug("Unexpected verack, already received.");
            //TODO bad behavior, call peer score manager
         }
         else {
            this.status.VersionAckReceived = true;
            if (this.status.VersionReceived) {
               this.PeerContext.NegotiatedProtocolVersion.Version = Math.Min(this.status.PeerVersion.Version, this.nodeImplementation.ImplementationVersion);
               await this.OnHandshaked().ConfigureAwait(false);
            }
            else {
               this.logger.LogDebug("Waiting version message...");
            }
         }
      }

      private async Task StartHandshakeAsync(CancellationToken cancellation) {
         VersionMessage version = this.status.PeerVersion;

         if (this.VersionNotSupported(version)) {
            throw new ProtocolViolationException("Peer version not supported.");
         }

         if (this.ConnectedToSelf(version)) {
            throw new ProtocolViolationException("Connection to self detected.");
         }

         // check the address used to connect to local node, in order to track how the node is seen outside (external address tracker)
         if (this.PeerContext.Direction == PeerConnectionDirection.Inbound && version.ReceiverAddress.EndPoint.Address.IsRoutable(true)) {
            //todo
            //   SeenLocal(addrMe);
         }

         this.logger.LogDebug("Responding to handshake with local Version.");
         await this.messageWriter.WriteAsync(this.CreateVersionMessage()).ConfigureAwait(false);
         await this.messageWriter.WriteAsync(new VerackMessage()).ConfigureAwait(false);

         await this.OnHandshaked().ConfigureAwait(false);
      }

      private async Task OnHandshaked() {
         this.logger.LogDebug("Handshake successful");
         this.status.IsHandShaked = true;

         if (this.status.PeerVersion.Version >= KnownVersion.V31402) {
            await this.messageWriter.WriteAsync(new GetaddrMessage()).ConfigureAwait(false);
         }
      }


      private bool ConnectedToSelf(VersionMessage version) {
         if (this.selfConnectionTracker.IsSelfConnection(version.Nonce)) {
            this.logger.LogDebug("Connection to self detected.");
            return true;
         }
         return false;
      }

      private bool VersionNotSupported(VersionMessage version) {
         if (version.Version < this.nodeImplementation.MinimumSupportedVersion) {
            this.logger.LogDebug("Connected peer uses an older and unsupported version {PeerVersion}.", version.Version);
            return true;
         }
         return false;
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
               EndPoint = this.PeerContext.PublicEndPoint ?? this.PeerContext.LocalEndPoint,
            },
            Relay = true, //this.IsRelay, TODO: it's part of the node settings
            Services = (ulong)NodeServices.Network // TODO: it's part of the node settings and depends on the configured features/shards
         };

         return version;
      }
   }
}
