using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Network.Network.Server {
   public class ServerPeer : IServerPeer, IDisposable {
      /// <summary>TCP server listener accepting inbound connections.</summary>
      private readonly ForgeTcpListener tcpListener;
      private readonly SubscriptionToken onPeerDisconnectedSubscription;

      /// <summary>
      /// Cancellation source that is triggered on dispose.
      /// </summary>
      private readonly CancellationTokenSource listenerCancellation;

      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly IPeerConnectionFactory peerConnectionFactory;
      private readonly Dictionary<Guid, IPeerConnection> connectedPeers;

      /// <summary>
      /// IP address and port, on which the server listens to incoming connections.
      /// </summary>
      /// <value>
      /// The local endpoint.
      /// </value>
      public IPEndPoint LocalEndPoint { get; }

      /// <summary>IP address and port of the external network interface that is accessible from the Internet.</summary>
      public IPEndPoint RemoteEndPoint { get; }



      public ServerPeer(ILogger<ServerPeer> logger,
                        IEventBus eventBus,
                        IPEndPoint localEndPoint,
                        IPEndPoint remoteEndPoint,
                        IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                        IPeerConnectionFactory peerConnectionFactory) {

         this.logger = logger;
         this.eventBus = eventBus;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.peerConnectionFactory = peerConnectionFactory;
         this.LocalEndPoint = localEndPoint.EnsureIPv6();
         this.RemoteEndPoint = remoteEndPoint.EnsureIPv6();

         this.connectedPeers = new Dictionary<Guid, IPeerConnection>();

         this.listenerCancellation = new CancellationTokenSource();

         this.tcpListener = new ForgeTcpListener(this.LocalEndPoint);

         this.onPeerDisconnectedSubscription = this.eventBus.Subscribe<PeerDisconnected>(this.OnPeerDisconnected);
      }

      private void OnPeerDisconnected(PeerDisconnected @event) {
         // This event is catched by every ServerPeer instance, so if we have multiple endpoints listening, all of them will
         // try to remove the item from the dictionary.
         if (this.connectedPeers.Remove(@event.PeerId)) {
            this.logger.LogDebug("Peer {PeerId} disconnected, removed from connectedPeers", @event.PeerId);
         }
      }


      /// <summary>
      /// Starts listening on the server's initialized endpoint.
      /// </summary>
      public async Task ListenAsync(CancellationToken cancellation) {
         using (this.logger.BeginScope("Listener {LocalEndpoint}", this.tcpListener.LocalEndpoint)) {
            try {
               this.tcpListener.Start();
               await this.AcceptClientsAsync(cancellation).ConfigureAwait(false);
            }
            catch (Exception e) {
               this.logger.LogCritical(e, "Listen exception occurred.");
               throw;
            }
         }
      }

      /// <inheritdoc />
      public void StopListening() {
         if (this.tcpListener.IsActive) {
            this.logger.LogInformation("Stopping listening to {EndPoint}", this.LocalEndPoint);
            this.tcpListener.Stop();
            this.listenerCancellation.Cancel();
         }
      }

      /// <summary>
      /// Implements loop accepting connections from newly connected clients.
      /// </summary>
      private async Task AcceptClientsAsync(CancellationToken cancellationToken) {
         this.logger.LogDebug("Accepting incoming connections.");

         try {
            while (!this.listenerCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {

               TcpClient connectingPeer = await this.tcpListener.AcceptTcpClientAsync()
                  .WithCancellationAsync(this.listenerCancellation.Token)
                  .ConfigureAwait(false);

               ServerPeerConnectionGuardResult connectionGuardResult = this.EnsurePeerCanConnect(connectingPeer);

               if (connectionGuardResult.IsDenied) {
                  this.logger.LogDebug("Connection from client '{ConnectingPeerEndPoint}' was rejected and will be closed.", connectingPeer.Client.RemoteEndPoint);
                  connectingPeer.Close();
                  continue;
               }

               //spawn a new task to manage the peer connection
               Task.Run(async () => {
                  await this.EstablishConnection(connectingPeer, cancellationToken).ConfigureAwait(false);
               });
            }
         }
         catch (OperationCanceledException) {
            this.logger.LogDebug("Shutdown detected, stop accepting connections.");
         }
         catch (Exception e) {
            this.logger.LogDebug("Exception occurred: {0}", e.ToString());
         }
         finally {
            this.StopListening();
         }
      }

      private async Task EstablishConnection(TcpClient connectingPeer, CancellationToken cancellationToken) {
         LoggerExtensions.LogDebug(this.logger, (string)"Connection accepted from client '{ConnectingPeerEndPoint}'.", connectingPeer.Client.RemoteEndPoint);

         IPeerConnection acceptedPeer = this.peerConnectionFactory.CreatePeerConnection((TcpClient)connectingPeer, cancellationToken);
         this.connectedPeers[acceptedPeer.PeerConnectionId] = acceptedPeer;

         try {
            await acceptedPeer.IncomingConnectionAccepted(cancellationToken).ConfigureAwait(false);
         }
         catch (Exception ex) {
            this.logger.LogCritical(ex, "Should never happen, need to be fixed!");
         }
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private ServerPeerConnectionGuardResult EnsurePeerCanConnect(TcpClient tcpClient) {
         if (this.serverPeerConnectionGuards == null) {
            return ServerPeerConnectionGuardResult.Success;
         }

         IPeerContext peerContext = new PeerContext((IPEndPoint)tcpClient.Client.LocalEndPoint, (IPEndPoint)tcpClient.Client.RemoteEndPoint);

         return (
            from guard in this.serverPeerConnectionGuards
            let guardResult = guard.Check(peerContext)
            where guardResult.IsDenied
            select guardResult
            )
            .DefaultIfEmpty(ServerPeerConnectionGuardResult.Success)
            .FirstOrDefault();
      }

      public void Dispose() {
         this.onPeerDisconnectedSubscription.Dispose();
      }
   }
}
