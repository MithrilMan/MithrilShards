using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MithrilShards.Network.Bedrock {
   public class MithrilForgeConnectionHandler : ConnectionHandler {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      private readonly IChainDefinition chainDefinition;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;

      public MithrilForgeConnectionHandler(ILogger<MithrilForgeConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IEventBus eventBus,
                                           IChainDefinition chainDefinition,
                                           IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                           INetworkMessageSerializerManager networkMessageSerializerManager) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.chainDefinition = chainDefinition;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         IPeerContext peerContext = new PeerContext(PeerConnectionDirection.Inbound,
                                                    connection.ConnectionId,
                                                    connection.LocalEndPoint,
                                                    connection.RemoteEndPoint);
         connection.Items[nameof(IPeerContext)] = peerContext;

         using (this.logger.BeginScope("Peer connected to server {ServerEndpoint}", connection.LocalEndPoint)) {

            this.eventBus.Publish(new PeerConnectionAttempt(peerContext));

            this.EnsurePeerCanConnect(connection, peerContext);

            this.eventBus.Publish(new PeerConnected(peerContext));

            var contextData = new ConnectionContextData(this.chainDefinition.MagicBytes);

            var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                      this.chainDefinition,
                                                      this.networkMessageSerializerManager,
                                                      contextData);

            ProtocolReader<INetworkMessage> reader = Protocol.CreateReader(connection, protocol);
            ProtocolWriter<INetworkMessage> writer = Protocol.CreateWriter(connection, protocol);

            while (true) {
               INetworkMessage message = await reader.ReadAsync();

               // REVIEW: We need a ReadResult<T> to indicate completion and cancellation
               if (message == null) {
                  break;
               }

               await this.ProcessMessage(message, connection, contextData, peerContext).ConfigureAwait(false);
            }

            this.eventBus.Publish(new PeerDisconnected(peerContext, "Client disconnected", null));
         }
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private void EnsurePeerCanConnect(ConnectionContext connection, IPeerContext peerContext) {
         if (this.serverPeerConnectionGuards == null) {
            return;
         }

         ServerPeerConnectionGuardResult result = (
            from guard in this.serverPeerConnectionGuards
            let guardResult = guard.Check(peerContext)
            where guardResult.IsDenied
            select guardResult
            )
            .DefaultIfEmpty(ServerPeerConnectionGuardResult.Success)
            .FirstOrDefault();

         if (result.IsDenied) {
            this.logger.LogDebug("Connection from client '{ConnectingPeerEndPoint}' was rejected because of {ClientDisconnectedReason} and will be closed.", connection.RemoteEndPoint, result.DenyReason);
            connection.Abort(new ConnectionAbortedException(result.DenyReason));
            this.eventBus.Publish(new PeerDisconnected(peerContext, result.DenyReason, null));
         }
      }

      private async Task ProcessMessage(INetworkMessage message, ConnectionContext connection, ConnectionContextData contextData, IPeerContext peerContext) {
         this.logger.LogInformation("Received a message of {Length} bytes", contextData.PayloadLength);
         this.logger.LogDebug("Parsing message '{Command}'", message.Command);

         if (message is UnknownMessage) {
            this.logger.LogWarning("Serializer for message '{Command}' not found.", message.Command);
         }
         else {
            this.eventBus.Publish(new PeerMessageReceived(peerContext, message, (int)contextData.GetTotalMessageLength()));
            this.logger.LogDebug(JsonSerializer.Serialize(message, message.GetType(), new JsonSerializerOptions { WriteIndented = true }));
         }
      }
   }
}
