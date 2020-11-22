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
      private readonly HandshakeProcessorStatus status;
      private readonly IDateTimeProvider dateTimeProvider;
      private readonly IRandomNumberGenerator randomNumberGenerator;
      private readonly NodeImplementation nodeImplementation;
      private readonly IInitialBlockDownloadTracker initialBlockDownloadState;
      private readonly IUserAgentBuilder userAgentBuilder;
      readonly ILocalServiceProvider localServiceProvider;
      readonly IHeadersTree headersTree;
      private readonly SelfConnectionTracker selfConnectionTracker;

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
         this.dateTimeProvider = dateTimeProvider;
         this.randomNumberGenerator = randomNumberGenerator;
         this.nodeImplementation = nodeImplementation;
         this.initialBlockDownloadState = initialBlockDownloadState;
         this.userAgentBuilder = userAgentBuilder;
         this.localServiceProvider = localServiceProvider;
         this.headersTree = headersTree;
         this.selfConnectionTracker = selfConnectionTracker;
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
         bool peerServiceSupports(NodeServices service)
         {
            return (version.Services & (ulong)service) != 0;
         }

         // did peers already handshaked?
         if (this.status.IsHandShaked)
         {
            this.logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         // did our peer received already peer version?
         if (this.status.PeerVersion != null)
         {
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            this.Misbehave(1, "Version message already received, expected only one.");
            return false;
         }

         /// we wants to connect only to peer that have the required services.
         /// Not enforcing the rule for other kind of connections
         /// TODO: consider excluding from this rule the peer we manual connects to
         /// see https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1935-L1940
         if (this.PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            if (this.PeerDoesntOfferRequiredServices(version)) throw new ProtocolViolationException("Peer does not offer the expected services.");
         }

         if (this.VersionNotSupported(version)) throw new ProtocolViolationException("Peer version not supported.");
         if (this.ConnectedToSelf(version)) throw new ProtocolViolationException("Connection to self detected.");

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

         if (!peerServiceSupports(NodeServices.Network))
         {
            if (!peerServiceSupports(NodeServices.NetworkLimited))
            {
               this.PeerContext.IsClient = true;
            }
            else
            {
               this.PeerContext.IsLimitedNode = true;
            }
         }

         this.PeerContext.CanServeWitness = peerServiceSupports(NodeServices.Witness);

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
         NodeServices requiredServices = this.initialBlockDownloadState.IsDownloadingBlocks() ?
            (NodeServices.Network | NodeServices.Witness) : (NodeServices.NetworkLimited | NodeServices.Witness);

         var peerServices = (NodeServices)peerVersion.Services;

         return !peerServices.HasFlag(requiredServices);
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
            //https://github.com/bitcoin/bitcoin/blob/d9a45500018fa4fd52c9c9326f79521d93d99abb/src/net_processing.cpp#L1909-L1914
            this.Misbehave(1, "Received additional verack, a previous one has been received.");
            return false;
         }

         await this.status.VerAckReceivedAsync().ConfigureAwait(false);

         // will prevent to handle version messages to other Processors
         return false;
      }

      private bool ConnectedToSelf(VersionMessage version)
      {
         if (this.selfConnectionTracker.IsSelfConnection(version.Nonce))
         {
            this.logger.LogDebug("Connection to self detected.");
            return true;
         }
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
            /// TODO: it's part of the node settings and depends on the configured features/shards, shouldn't be hard coded
            /// if/when pruned will be implemented, remember to remove Network service flag
            /// ref: https://github.com/bitcoin/bitcoin/blob/99813a9745fe10a58bedd7a4cb721faf14f907a4/src/init.cpp#L1671-L1680
            Services = (ulong)this.localServiceProvider.GetServices(),
            Timestamp = this.dateTimeProvider.GetTimeOffset(),
            ReceiverAddress = new Types.NetworkAddressNoTime { EndPoint = this.PeerContext.RemoteEndPoint },
            SenderAddress = new Types.NetworkAddressNoTime { EndPoint = this.PeerContext.PublicEndPoint ?? this.PeerContext.LocalEndPoint },
            Nonce = this.randomNumberGenerator.GetUint64(),
            UserAgent = this.userAgentBuilder.GetUserAgent(),
            StartHeight = this.headersTree.GetTip().Height,
            Relay = true //this.IsRelay, TODO: it's part of the node settings
         };

         return version;
      }
   }
}
