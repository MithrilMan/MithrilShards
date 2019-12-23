using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
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
      private readonly IChainDefinition chainDefinition;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;

      public MithrilForgeConnectionHandler(ILogger<MithrilForgeConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IChainDefinition chainDefinition,
                                           IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                           INetworkMessageSerializerManager networkMessageSerializerManager) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.chainDefinition = chainDefinition;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {

         this.EnsurePeerCanConnect(connection);

         var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                   this.chainDefinition,
                                                   this.networkMessageSerializerManager);

         ProtocolReader<Message> reader = Protocol.CreateReader(connection, protocol);
         ProtocolWriter<Message> writer = Protocol.CreateWriter(connection, protocol);

         while (true) {
            Message message = await reader.ReadAsync();

            this.logger.LogInformation("Received a message of {Length} bytes", message.Payload.Length);

            // REVIEW: We need a ReadResult<T> to indicate completion and cancellation
            if (message.Payload == null) {
               break;
            }

            await this.ParseMessage(message, connection).ConfigureAwait(false);
         }
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private void EnsurePeerCanConnect(ConnectionContext connection) {
         if (this.serverPeerConnectionGuards == null) {
            return;
         }

         IPeerContext peerContext = new PeerContext((IPEndPoint)connection.LocalEndPoint, (IPEndPoint)connection.RemoteEndPoint);

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
         }
      }

      private async Task ParseMessage(Message message, ConnectionContext connection) {
         string command = Encoding.ASCII.GetString(message.Command.Trim((byte)'\0'));
         this.logger.LogDebug("Message '{Command}'", command);

         //TODO instead of returning a Message, it should already return a typed known network message
         if (this.networkMessageSerializerManager.Serializers.TryGetValue(command.ToLowerInvariant(), out INetworkMessageSerializer serializer)) {
            INetworkMessage msg = serializer.Deserialize(message.Payload);
            this.logger.LogDebug(
               JsonSerializer.Serialize(msg, serializer.GetMessageType(), new JsonSerializerOptions { WriteIndented = true })
            );
         }
         else {
            this.logger.LogWarning("Serializer for message '{Command}' not found.", command);
         }
      }
   }
}
