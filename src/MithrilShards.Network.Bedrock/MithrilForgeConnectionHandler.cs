using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Network.Server.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Network.Bedrock {
   public class MithrilForgeConnectionHandler : ConnectionHandler {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      private readonly IChainDefinition chainDefinition;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      readonly IPeerContextFactory peerContextFactory;
      readonly ForgeServerSettings serverSettings;

      public MithrilForgeConnectionHandler(ILogger<MithrilForgeConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IEventBus eventBus,
                                           IChainDefinition chainDefinition,
                                           IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                           INetworkMessageSerializerManager networkMessageSerializerManager,
                                           INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                           IPeerContextFactory peerContextFactory,
                                           IOptions<ForgeServerSettings> serverSettings) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.chainDefinition = chainDefinition;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.peerContextFactory = peerContextFactory;
         this.serverSettings = serverSettings?.Value;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         if (connection is null) {
            throw new ArgumentNullException(nameof(connection));
         }

         using (this.logger.BeginScope("Peer connected to server {ServerEndpoint}", connection.LocalEndPoint)) {
            var contextData = new ConnectionContextData(this.chainDefinition.MagicBytes);
            var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                      this.chainDefinition,
                                                      this.networkMessageSerializerManager,
                                                      contextData);

            ProtocolReader<INetworkMessage> reader = Protocol.CreateReader(connection, protocol);
            ProtocolWriter<INetworkMessage> writer = Protocol.CreateWriter(connection, protocol);

            using (IPeerContext peerContext = this.peerContextFactory.Create(PeerConnectionDirection.Inbound,
                                                    connection.ConnectionId,
                                                    connection.LocalEndPoint,
                                                    connection.RemoteEndPoint,
                                                    new NetworkMessageWriter(writer))) {

               connection.Features.Set(peerContext);
               protocol.SetPeerContext(peerContext);

               this.eventBus.Publish(new PeerConnectionAttempt(peerContext));
               if (this.EnsurePeerCanConnect(connection, peerContext)) {

                  this.eventBus.Publish(new PeerConnected(peerContext));

                  this.networkMessageProcessorFactory.StartProcessorsAsync(peerContext);

                  while (true) {
                     INetworkMessage message = await reader.ReadAsync().ConfigureAwait(false);

                     // REVIEW: We need a ReadResult<T> to indicate completion and cancellation
                     if (message == null) {
                        break;
                     }

                     await this.ProcessMessage(message, connection, contextData, peerContext, writer).ConfigureAwait(false);
                  }
                  return;
               }

               this.eventBus.Publish(new PeerDisconnected(peerContext, "Client disconnected", null));
            }
         }
      }

      private void DisposePeerContext(ConnectionContext connection) {
         IPeerContext peerContext = connection.Features.Get<IPeerContext>();
         if (peerContext != null) {
            peerContext.Dispose();
         }
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private bool EnsurePeerCanConnect(ConnectionContext connection, IPeerContext peerContext) {
         if (this.serverPeerConnectionGuards == null) {
            return false;
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
            return false;
         }

         return true;
      }

      private async Task ProcessMessage(INetworkMessage message,
                                        ConnectionContext connection,
                                        ConnectionContextData contextData,
                                        IPeerContext peerContext,
                                        ProtocolWriter<INetworkMessage> writer) {
         this.logger.LogInformation("Received a message of {Length} bytes", contextData.PayloadLength);
         this.logger.LogDebug("Parsing message '{Command}'", message.Command);

         if (message is UnknownMessage) {
            this.logger.LogWarning("Serializer for message '{Command}' not found.", message.Command);
         }
         else {
            this.logger.LogDebug(JsonSerializer.Serialize(message, message.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            this.eventBus.Publish(new PeerMessageReceived(peerContext, message, (int)contextData.GetTotalMessageLength()));

            await peerContext.ProcessMessageAsync(message).ConfigureAwait(false);
         }
      }
   }
}
