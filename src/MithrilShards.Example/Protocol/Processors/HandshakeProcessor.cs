using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;

namespace MithrilShards.Example.Protocol.Processors;

public partial class HandshakeProcessor : BaseProcessor,
   INetworkMessageHandler<VersionMessage>,
   INetworkMessageHandler<VerackMessage>
{
   const int HANDSHAKE_TIMEOUT_SECONDS = 5;
   private readonly HandshakeProcessorStatus _status;
   private readonly IDateTimeProvider _dateTimeProvider;
   private readonly IRandomNumberGenerator _randomNumberGenerator;
   private readonly NodeImplementation _nodeImplementation;
   private readonly IUserAgentBuilder _userAgentBuilder;

   public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                             IEventBus eventBus,
                             IDateTimeProvider dateTimeProvider,
                             IRandomNumberGenerator randomNumberGenerator,
                             NodeImplementation nodeImplementation,
                             IPeerBehaviorManager peerBehaviorManager,
                             IUserAgentBuilder userAgentBuilder
                             ) : base(logger,
                                      eventBus,
                                      peerBehaviorManager,
                                      isHandshakeAware: true,
                                      // we are performing handshake so we want to receive messages before handshake status
                                      receiveMessagesOnlyIfHandshaked: false)
   {
      _dateTimeProvider = dateTimeProvider;
      _randomNumberGenerator = randomNumberGenerator;
      _nodeImplementation = nodeImplementation;
      _userAgentBuilder = userAgentBuilder;
      _status = new HandshakeProcessorStatus(this);
   }

   protected override async ValueTask OnPeerAttachedAsync()
   {
      //add the status to the PeerContext, this way other processors may query the status
      PeerContext.Features.Set(_status);

      // ensures the handshake is performed timely
      _ = DisconnectIfAsync(() =>
      {
         return new ValueTask<bool>(_status.IsHandShaked == false);
      }, TimeSpan.FromSeconds(HANDSHAKE_TIMEOUT_SECONDS), "Handshake not performed in time");

      if (PeerContext.Direction == PeerConnectionDirection.Outbound)
      {
         logger.LogDebug("Commencing handshake with local Version.");
         await SendMessageAsync(CreateVersionMessage()).ConfigureAwait(false);
         _status.VersionSent();
      }
   }

   async ValueTask<bool> INetworkMessageHandler<VersionMessage>.ProcessMessageAsync(VersionMessage version, CancellationToken cancellation)
   {
      // did peers already handshaked?
      if (_status.IsHandShaked)
      {
         logger.LogDebug("Receiving version while already handshaked, disconnect.");
         throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
      }

      // did our peer received already peer version?
      if (_status.PeerVersion != null)
      {
         Misbehave(1, "Version message already received, expected only one.");
         return false;
      }

      if (VersionNotSupported(version)) throw new ProtocolViolationException("Peer version not supported.");

      // first time we receive version
      await _status.VersionReceivedAsync(version).ConfigureAwait(false);

      if (PeerContext.Direction == PeerConnectionDirection.Inbound)
      {
         logger.LogDebug("Responding to handshake with local Version.");
         await SendMessageAsync(CreateVersionMessage()).ConfigureAwait(false);
         _status.VersionSent();
      }

      await SendMessageAsync(new VerackMessage()).ConfigureAwait(false);

      PeerContext.TimeOffset = _dateTimeProvider.GetTimeOffset() - version.Timestamp;

      // will prevent to handle version messages to other Processors
      return false;
   }

   async ValueTask<bool> INetworkMessageHandler<VerackMessage>.ProcessMessageAsync(VerackMessage verack, CancellationToken cancellation)
   {
      if (!_status.IsVersionSent)
      {
         Misbehave(10, "Received verack without having sent a version.");
         return false;
      }

      if (_status.VersionAckReceived)
      {
         Misbehave(1, "Received additional verack, a previous one has been received.");
         return false;
      }

      await _status.VerAckReceivedAsync().ConfigureAwait(false);

      // will prevent to handle version messages to other Processors
      return false;
   }

   private bool VersionNotSupported(VersionMessage version)
   {
      if (version.Version < _nodeImplementation.MinimumSupportedVersion)
      {
         logger.LogDebug("Connected peer uses an older and unsupported version {PeerVersion}.", version.Version);
         return true;
      }
      return false;
   }

   private VersionMessage CreateVersionMessage()
   {
      var version = new VersionMessage
      {
         Version = KnownVersion.CurrentVersion,
         Timestamp = _dateTimeProvider.GetTimeOffset(),
         Nonce = _randomNumberGenerator.GetUint64(),
         UserAgent = _userAgentBuilder.GetUserAgent(),
      };

      return version;
   }
}
