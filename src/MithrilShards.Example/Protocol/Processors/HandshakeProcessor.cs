using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Example.Protocol.Processors
{
   public partial class HandshakeProcessor : BaseProcessor,
      INetworkMessageHandler<VersionMessage>,
      INetworkMessageHandler<VerackMessage>
   {
      const int HANDSHAKE_TIMEOUT_SECONDS = 5;
      private readonly HandshakeProcessorStatus status;
      private readonly IDateTimeProvider dateTimeProvider;
      private readonly IRandomNumberGenerator randomNumberGenerator;
      private readonly NodeImplementation nodeImplementation;
      private readonly IUserAgentBuilder userAgentBuilder;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IDateTimeProvider dateTimeProvider,
                                IRandomNumberGenerator randomNumberGenerator,
                                NodeImplementation nodeImplementation,
                                IPeerBehaviorManager peerBehaviorManager,
                                IUserAgentBuilder userAgentBuilder
                                ) : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.dateTimeProvider = dateTimeProvider;
         this.randomNumberGenerator = randomNumberGenerator;
         this.nodeImplementation = nodeImplementation;
         this.userAgentBuilder = userAgentBuilder;
         this.status = new HandshakeProcessorStatus(this);
      }

      protected override async ValueTask OnPeerAttachedAsync()
      {
         //add the status to the PeerContext, this way other processors may query the status
         this.PeerContext.Features.Set(this.status);

         // ensures the handshake is performed timely
         _ = this.DisconnectIfAsync(() =>
         {
            return new ValueTask<bool>(this.status.IsHandShaked == false);
         }, TimeSpan.FromSeconds(HANDSHAKE_TIMEOUT_SECONDS), "Handshake not performed in time");

         if (this.PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            this.logger.LogDebug("Commencing handshake with local Version.");
            await this.SendMessageAsync(this.CreateVersionMessage()).ConfigureAwait(false);
            this.status.VersionSent();
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(VersionMessage version, CancellationToken cancellation)
      {
         // did peers already handshaked?
         if (this.status.IsHandShaked)
         {
            this.logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         // did our peer received already peer version?
         if (this.status.PeerVersion != null)
         {
            this.Misbehave(1, "Version message already received, expected only one.");
            return false;
         }

         if (this.VersionNotSupported(version)) throw new ProtocolViolationException("Peer version not supported.");

         // first time we receive version
         await this.status.VersionReceivedAsync(version).ConfigureAwait(false);

         if (this.PeerContext.Direction == PeerConnectionDirection.Inbound)
         {
            this.logger.LogDebug("Responding to handshake with local Version.");
            await this.SendMessageAsync(this.CreateVersionMessage()).ConfigureAwait(false);
            this.status.VersionSent();
         }

         await this.SendMessageAsync(new VerackMessage()).ConfigureAwait(false);

         this.PeerContext.TimeOffset = this.dateTimeProvider.GetTimeOffset() - version.Timestamp;

         // will prevent to handle version messages to other Processors
         return false;
      }

      public async ValueTask<bool> ProcessMessageAsync(VerackMessage verack, CancellationToken cancellation)
      {
         if (!this.status.IsVersionSent)
         {
            this.Misbehave(10, "Received verack without having sent a version.");
            return false;
         }

         if (this.status.VersionAckReceived)
         {
            this.Misbehave(1, "Received additional verack, a previous one has been received.");
            return false;
         }

         await this.status.VerAckReceivedAsync().ConfigureAwait(false);

         // will prevent to handle version messages to other Processors
         return false;
      }

      private bool VersionNotSupported(VersionMessage version)
      {
         if (version.Version < this.nodeImplementation.MinimumSupportedVersion)
         {
            this.logger.LogDebug("Connected peer uses an older and unsupported version {PeerVersion}.", version.Version);
            return true;
         }
         return false;
      }

      private VersionMessage CreateVersionMessage()
      {
         var version = new VersionMessage
         {
            Version = KnownVersion.CurrentVersion,
            Timestamp = this.dateTimeProvider.GetTimeOffset(),
            Nonce = this.randomNumberGenerator.GetUint64(),
            UserAgent = this.userAgentBuilder.GetUserAgent(),
         };

         return version;
      }
   }
}
