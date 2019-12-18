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
using MithrilShards.P2P.Helpers;
using MithrilShards.P2P.Network.Server.Guards;

namespace MithrilShards.P2P.Network.Server {
   public class ServerPeer : IServerPeer {
      /// <summary>TCP server listener accepting inbound connections.</summary>
      private readonly ForgeTcpListener tcpListener;

      /// <summary>
      /// Cancellation source that is triggered on dispose.
      /// </summary>
      private readonly CancellationTokenSource listenerCancellation;

      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;

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
                        IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards) {

         this.logger = logger;
         this.eventBus = eventBus;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.LocalEndPoint = localEndPoint.EnsureIPv6();
         this.RemoteEndPoint = remoteEndPoint.EnsureIPv6();

         this.listenerCancellation = new CancellationTokenSource();

         this.tcpListener = new ForgeTcpListener(this.LocalEndPoint);
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
                  .WithCancellationAsync(cancellationToken)
                  .ConfigureAwait(false);

               ServerPeerConnectionGuardResult connectionGuardResult = this.EnsurePeerCanConnect(connectingPeer);

               if (connectionGuardResult.IsDenied) {
                  this.logger.LogDebug("Connection from client '{ConnectingPeerEndPoint}' was rejected and will be closed.", connectingPeer.Client.RemoteEndPoint);
                  connectingPeer.Close();
                  continue;
               }

               this.EstablishConnection(connectingPeer);
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

      private void EstablishConnection(TcpClient connectingPeer) {
         this.logger.LogDebug("Connection accepted from client '{ConnectingPeerEndPoint}'.", connectingPeer.Client.RemoteEndPoint);

         this.fact
         var peer = new NetworkPeer((IPEndPoint)client.Client.RemoteEndPoint, this.network, parameters, client, this.dateTimeProvider, this, this.loggerFactory, this.selfEndpointTracker, this.asyncProvider, onDisconnected, this.onSendingMessage);

         this.eventBus.Publish(new Events.PeerConnected(true, (IPEndPoint)connectingPeer.Client.RemoteEndPoint));
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private ServerPeerConnectionGuardResult EnsurePeerCanConnect(TcpClient tcpClient) {
         if (this.serverPeerConnectionGuards == null) {
            return ServerPeerConnectionGuardResult.Success;
         }

         return (
            from guard in this.serverPeerConnectionGuards
            let guardResult = guard.Check(tcpClient)
            where guardResult.IsDenied
            select guardResult
            )
            .DefaultIfEmpty(ServerPeerConnectionGuardResult.Success)
            .FirstOrDefault();
      }
   }
}
