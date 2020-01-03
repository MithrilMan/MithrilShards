using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Network.Bedrock {
   public class MithrilForgeServerConnectionHandler : ConnectionHandler {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      private readonly IChainDefinition chainDefinition;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      readonly IPeerContextFactory peerContextFactory;
      readonly ForgeConnectivitySettings connectivitySettings;

      public MithrilForgeServerConnectionHandler(ILogger<MithrilForgeServerConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IEventBus eventBus,
                                           IChainDefinition chainDefinition,
                                           IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                           INetworkMessageSerializerManager networkMessageSerializerManager,
                                           INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                           IPeerContextFactory peerContextFactory,
                                           IOptions<ForgeConnectivitySettings> connectivitySettings) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.chainDefinition = chainDefinition;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.peerContextFactory = peerContextFactory;
         this.connectivitySettings = connectivitySettings?.Value;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         if (connection is null) {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable loggerScope = this.logger.BeginScope("Peer connected to server {ServerEndpoint}", connection.LocalEndPoint);
         var contextData = new ConnectionContextData(this.chainDefinition.MagicBytes);
         var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                   this.chainDefinition,
                                                   this.networkMessageSerializerManager,
                                                   contextData);

         ProtocolReader reader = connection.CreateReader();
         ProtocolWriter writer = connection.CreateWriter();

         using IPeerContext peerContext = this.peerContextFactory.Create(PeerConnectionDirection.Inbound,
                                                 connection.ConnectionId,
                                                 connection.LocalEndPoint,
                                                 connection.RemoteEndPoint,
                                                 new NetworkMessageWriter(protocol, writer));
         using CancellationTokenRegistration cancellationRegistration = peerContext.ConnectionCancellationTokenSource.Token.Register(() => {
            connection.Abort(new ConnectionAbortedException("Requested by PeerContext"));
         });

         connection.Features.Set(peerContext);
         protocol.SetPeerContext(peerContext);

         if (this.EnsurePeerCanConnect(connection, peerContext)) {

            this.eventBus.Publish(new PeerConnected(peerContext));

            await this.networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);

            while (true) {
               try {
                  ReadResult<INetworkMessage> result = await reader.ReadAsync(protocol).ConfigureAwait(false);

                  if (result.IsCompleted) {
                     break;
                  }

                  await this.ProcessMessage(result.Message, connection, contextData, peerContext).ConfigureAwait(false);
               }
               catch (Exception ex) {
                  this.logger.LogDebug(ex, "Unexpected connection terminated because of {DisconnectionReason}.", ex.Message);
                  break;
               }
               finally {
                  reader.Advance();
               }
            }

            this.eventBus.Publish(new PeerDisconnected(peerContext, "Client disconnected", null));
            return;
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
            this.eventBus.Publish(new PeerConnectionAttemptFailed(peerContext, result.DenyReason));
            return false;
         }

         return true;
      }

      private async Task ProcessMessage(INetworkMessage message,
                                        ConnectionContext connection,
                                        ConnectionContextData contextData,
                                        IPeerContext peerContext) {

         using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", message.Command);
         this.logger.LogDebug("Parsing message '{Command}' with size of {PayloadSize}", message.Command, contextData.PayloadLength);

         if (!(message is UnknownMessage)) {
            await peerContext.ProcessMessageAsync(message).ConfigureAwait(false);
            this.eventBus.Publish(new PeerMessageReceived(peerContext, message, contextData.GetTotalMessageLength()));
         }
      }
   }
}
