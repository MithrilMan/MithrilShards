﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class HandshakeProcessor : BaseProcessor,
      INetworkMessageHandler<VersionMessage>,
      INetworkMessageHandler<VerackMessage>
   {
      const int HANDSHAKE_TIMEOUT_SECONDS = 5;
      private readonly HandshakeProcessorStatus _status;
      private readonly IDateTimeProvider _dateTimeProvider;
      private readonly IRandomNumberGenerator _randomNumberGenerator;
      private readonly NodeImplementation _nodeImplementation;
      private readonly IInitialBlockDownloadTracker _initialBlockDownloadState;
      private readonly IUserAgentBuilder _userAgentBuilder;
      readonly ILocalServiceProvider _localServiceProvider;
      readonly IHeadersTree _headersTree;
      private readonly SelfConnectionTracker _selfConnectionTracker;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IDateTimeProvider dateTimeProvider,
                                IRandomNumberGenerator randomNumberGenerator,
                                NodeImplementation nodeImplementation,
                                IPeerBehaviorManager peerBehaviorManager,
                                IInitialBlockDownloadTracker initialBlockDownloadState,
                                IUserAgentBuilder userAgentBuilder,
                                ILocalServiceProvider localServiceProvider,
                                IHeadersTree headersTree,
                                SelfConnectionTracker selfConnectionTracker) : base(logger,
                                                                                    eventBus,
                                                                                    peerBehaviorManager,
                                                                                    isHandshakeAware: true,
                                                                                    // we are performing handshake so we want to receive messages before handshake status
                                                                                    receiveMessagesOnlyIfHandshaked: false)
      {
         _dateTimeProvider = dateTimeProvider;
         _randomNumberGenerator = randomNumberGenerator;
         _nodeImplementation = nodeImplementation;
         _initialBlockDownloadState = initialBlockDownloadState;
         _userAgentBuilder = userAgentBuilder;
         _localServiceProvider = localServiceProvider;
         _headersTree = headersTree;
         _selfConnectionTracker = selfConnectionTracker;
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
         bool peerServiceSupports(NodeServices service)
         {
            return (version.Services & (ulong)service) != 0;
         }

         // did peers already handshaked?
         if (_status.IsHandShaked)
         {
            logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         // did our peer received already peer version?
         if (_status.PeerVersion != null)
         {
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            Misbehave(1, "Version message already received, expected only one.");
            return false;
         }

         /// we wants to connect only to peer that have the required services.
         /// Not enforcing the rule for other kind of connections
         /// TODO: consider excluding from this rule the peer we manual connects to
         /// see https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1935-L1940
         if (PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            if (PeerDoesntOfferRequiredServices(version)) throw new ProtocolViolationException("Peer does not offer the expected services.");
         }

         if (VersionNotSupported(version)) throw new ProtocolViolationException("Peer version not supported.");
         if (ConnectedToSelf(version)) throw new ProtocolViolationException("Connection to self detected.");

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

         if (!peerServiceSupports(NodeServices.Network))
         {
            if (!peerServiceSupports(NodeServices.NetworkLimited))
            {
               PeerContext.IsClient = true;
            }
            else
            {
               PeerContext.IsLimitedNode = true;
            }
         }

         PeerContext.CanServeWitness = peerServiceSupports(NodeServices.Witness);

         // will prevent to handle version messages to other Processors
         return false;
      }

      /// <summary>
      /// Return false if peer doesn't offer required services.
      /// </summary>
      /// <param name="peerVersion">The peer version.</param>
      /// <returns><see langword="false"/> if peer doesn't offer required service, <see langword="true"/> otherwise.</returns>
      private bool PeerDoesntOfferRequiredServices(VersionMessage peerVersion)
      {
         NodeServices requiredServices = _initialBlockDownloadState.IsDownloadingBlocks() ?
            (NodeServices.Network | NodeServices.Witness) : (NodeServices.NetworkLimited | NodeServices.Witness);

         var peerServices = (NodeServices)peerVersion.Services;

         return !peerServices.HasFlag(requiredServices);
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
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            Misbehave(1, "Received additional verack, a previous one has been received.");
            return false;
         }

         await _status.VerAckReceivedAsync().ConfigureAwait(false);

         // will prevent to handle version messages to other Processors
         return false;
      }

      private bool ConnectedToSelf(VersionMessage version)
      {
         if (_selfConnectionTracker.IsSelfConnection(version.Nonce))
         {
            logger.LogDebug("Connection to self detected.");
            return true;
         }
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
            /// TODO: it's part of the node settings and depends on the configured features/shards, shouldn't be hard coded
            /// if/when pruned will be implemented, remember to remove Network service flag
            /// ref: https://github.com/bitcoin/bitcoin/blob/99813a9745fe10a58bedd7a4cb721faf14f907a4/src/init.cpp#L1671-L1680
            Services = (ulong)_localServiceProvider.GetServices(),
            Timestamp = _dateTimeProvider.GetTimeOffset(),
            ReceiverAddress = new Types.NetworkAddressNoTime { EndPoint = PeerContext.RemoteEndPoint },
            SenderAddress = new Types.NetworkAddressNoTime { EndPoint = PeerContext.PublicEndPoint ?? PeerContext.LocalEndPoint },
            Nonce = _randomNumberGenerator.GetUint64(),
            UserAgent = _userAgentBuilder.GetUserAgent(),
            StartHeight = _headersTree.GetTip().Height,
            Relay = true //this.IsRelay, TODO: it's part of the node settings
         };

         return version;
      }
   }
}
