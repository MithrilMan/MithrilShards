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
   public class HandshakeProcessor : BaseProcessor {
      internal class Status {
         private readonly HandshakeProcessor processor;

         internal VersionMessage PeerVersion { get; private set; } = null;

         internal bool IsHandShaked { get; private set; } = false;

         internal bool VersionAckReceived { get; private set; } = false;

         public Status(HandshakeProcessor processor) {
            this.processor = processor;
         }

         internal async ValueTask VersionReceivedAsync(VersionMessage version) {
            this.PeerVersion = version;
            this.processor.PeerContext.NegotiatedProtocolVersion.Version
               = Math.Min(this.PeerVersion.Version, this.processor.nodeImplementation.ImplementationVersion);

            await this.processor.SendMessageAsync(new VerackMessage()).ConfigureAwait(false);
            await this.OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         internal async ValueTask VerAckReceivedAsync() {
            this.VersionAckReceived = true;
            await this.OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         private async ValueTask OnHandshakeStatusUpdatedAsync() {
            if (!this.VersionAckReceived) {
               this.processor.logger.LogDebug("Waiting verack...");
               return;
            }

            if (this.PeerVersion == null) {
               this.processor.logger.LogDebug("Waiting version message...");
               return;
            }

            // if we reach this point, peer completed the handshake, yay!
            this.IsHandShaked = true;
            this.processor.logger.LogDebug("Handshake successful");
            if (this.PeerVersion.Version >= KnownVersion.V31402) {
               await this.processor.SendMessageAsync(new GetaddrMessage()).ConfigureAwait(false);
            }

            this.processor.eventBus.Publish(new PeerHandshaked(this.processor.PeerContext));
         }
      }

      private readonly Status status;
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
         this.status = new Status(this);
      }

      public override async ValueTask AttachAsync(IPeerContext peerContext) {
         await base.AttachAsync(peerContext).ConfigureAwait(false);

         // ensures the handshake is performed timely
         this.DisconnectIfAsync(() => {
            return this.status.IsHandShaked == false;
         }, TimeSpan.FromSeconds(5), "Handshake not performed in time");//this.PeerContext.Disconnected);

         if (peerContext.Direction == PeerConnectionDirection.Outbound) {
            this.logger.LogDebug("Commencing handshake with local Version.");
            await this.SendMessageAsync(this.CreateVersionMessage()).ConfigureAwait(false);
         }
      }


      public override async ValueTask<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation) {
         return message switch
         {
            VersionMessage version => await this.ProcessVersionMessageAsync(version, cancellation).ConfigureAwait(false),
            VerackMessage verack => await this.ProcessVerackMessageAsync(verack, cancellation).ConfigureAwait(false),
            _ => true
         };
      }

      private async Task<bool> ProcessVersionMessageAsync(VersionMessage version, CancellationToken cancellation) {
         // did peers already handshaked?
         if (this.status.IsHandShaked) {
            this.logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         // did our peer received already peer version?
         if (this.status.PeerVersion != null) {
            if (this.PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V70002) {
               var rejectMessage = new RejectMessage() {
                  Code = RejectMessage.RejectCode.Duplicate
               };

               await this.SendMessageAsync(rejectMessage, cancellation).ConfigureAwait(false);
               this.logger.LogWarning("Rejecting {MessageType}.", nameof(VersionMessage));
            }
            //TODO don't be so rude, apply a bad behavior score using peer score manager
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            throw new ProtocolViolationException("Version message already received, disconnecting because of protocol violation.");
         }

         // first time we receive version
         await this.status.VersionReceivedAsync(version).ConfigureAwait(false);

         if (this.PeerContext.Direction == PeerConnectionDirection.Inbound) {
            await this.StartHandshakeAsync(cancellation).ConfigureAwait(false);
         }

         this.PeerContext.TimeOffset = this.dateTimeProvider.GetTimeOffset() - version.Timestamp;
         if ((version.Services & (ulong)NodeServices.Witness) != 0) {
            //TODO
            // this.SupportedTransactionOptions |= TransactionOptions.Witness;
         }

         // will prevent to handle version messages to other Processors
         return false;
      }

      private async Task<bool> ProcessVerackMessageAsync(VerackMessage verack, CancellationToken cancellation) {
         if (this.status.VersionAckReceived) {
            this.logger.LogDebug("Unexpected verack, already received.");
            //TODO don't be so rude, apply a bad behavior score using peer score manager
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            throw new ProtocolViolationException("Unexpected verack, already received, disconnecting because of protocol violation.");
         }

         await this.status.VerAckReceivedAsync().ConfigureAwait(false);

         // will prevent to handle version messages to other Processors
         return false;
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
         await this.SendMessageAsync(new VerackMessage()).ConfigureAwait(false);
         await this.SendMessageAsync(this.CreateVersionMessage()).ConfigureAwait(false);
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
