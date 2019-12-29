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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Network.Bedrock {
   public class MithrilForgeClientConnectionHandler : ConnectionHandler {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      private readonly IChainDefinition chainDefinition;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      readonly IPeerContextFactory peerContextFactory;
      readonly ForgeConnectivitySettings connectivitySettings;

      public MithrilForgeClientConnectionHandler(ILogger<MithrilForgeClientConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IEventBus eventBus,
                                           IChainDefinition chainDefinition,
                                           INetworkMessageSerializerManager networkMessageSerializerManager,
                                           INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                           IPeerContextFactory peerContextFactory,
                                           IOptions<ForgeConnectivitySettings> connectivitySettings) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.chainDefinition = chainDefinition;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.peerContextFactory = peerContextFactory;
         this.connectivitySettings = connectivitySettings?.Value;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         if (connection is null) {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable logScope = this.logger.BeginScope("Peer {PeerId} connected to server {ServerEndpoint}", connection.LocalEndPoint);
         var contextData = new ConnectionContextData(this.chainDefinition.MagicBytes);
         var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                   this.chainDefinition,
                                                   this.networkMessageSerializerManager,
                                                   contextData);

         ProtocolReader reader = connection.CreateReader();
         ProtocolWriter writer = connection.CreateWriter();

         using IPeerContext peerContext = this.peerContextFactory.Create(PeerConnectionDirection.Outbound,
                                                 connection.ConnectionId,
                                                 connection.LocalEndPoint,
                                                 connection.RemoteEndPoint,
                                                 new NetworkMessageWriter(protocol, writer));

         connection.ConnectionClosed = peerContext.ConnectionCancellationTokenSource.Token;

         connection.Features.Set(peerContext);
         protocol.SetPeerContext(peerContext);

         this.eventBus.Publish(new PeerConnected(peerContext));

         await this.networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);

         while (true) {
            if (connection.ConnectionClosed.IsCancellationRequested) {
               break;
            }

            try {
               ReadResult<INetworkMessage> result = await reader.ReadAsync(protocol, connection.ConnectionClosed).ConfigureAwait(false);

               if (result.IsCompleted) {
                  break;
               }

               await this.ProcessMessage(result.Message, connection, contextData, peerContext)
                  .WithCancellationAsync(connection.ConnectionClosed)
                  .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) {
               break;
            }
            finally {
               reader.Advance();
            }
         }

         this.eventBus.Publish(new PeerDisconnected(peerContext, "Client disconnected", null));
         return;
      }

      private async Task ProcessMessage(INetworkMessage message,
                                        ConnectionContext connection,
                                        ConnectionContextData contextData,
                                        IPeerContext peerContext) {
         using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", message.Command);
         this.logger.LogDebug("Parsing message '{Command}' with size of {PayloadSize}", message.Command, contextData.PayloadLength);

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