﻿using System;
using System.Threading;
using System.Threading.Tasks;
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

namespace MithrilShards.Network.Bedrock
{
   public class MithrilForgeClientConnectionHandler : ConnectionHandler
   {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      private readonly INetworkDefinition chainDefinition;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      readonly IPeerContextFactory peerContextFactory;
      readonly ForgeConnectivitySettings connectivitySettings;

      public MithrilForgeClientConnectionHandler(ILogger<MithrilForgeClientConnectionHandler> logger,
                                           ILoggerFactory loggerFactory,
                                           IEventBus eventBus,
                                           INetworkDefinition chainDefinition,
                                           INetworkMessageSerializerManager networkMessageSerializerManager,
                                           INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                           IPeerContextFactory peerContextFactory,
                                           IOptions<ForgeConnectivitySettings> connectivitySettings)
      {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.chainDefinition = chainDefinition;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.peerContextFactory = peerContextFactory;
         this.connectivitySettings = connectivitySettings.Value;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection)
      {
         if (connection is null)
         {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable logScope = this.logger.BeginScope("Peer {PeerId} connected to server {ServerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);
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

         while (true)
         {
            if (connection.ConnectionClosed.IsCancellationRequested)
            {
               break;
            }

            try
            {
               ReadResult<INetworkMessage> result = await reader.ReadAsync(protocol, connection.ConnectionClosed).ConfigureAwait(false);

               if (result.IsCompleted)
               {
                  break;
               }

               await this.ProcessMessage(result.Message, contextData, peerContext, connection.ConnectionClosed)
                  .WithCancellationAsync(connection.ConnectionClosed)
                  .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
               break;
            }
            finally
            {
               reader.Advance();
            }
         }

         return;
      }

      private async Task ProcessMessage(INetworkMessage message,
                                        ConnectionContextData contextData,
                                        IPeerContext peerContext,
                                        CancellationToken cancellation)
      {
         using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", contextData.CommandName);
         this.logger.LogDebug("Parsing message '{Command}' with size of {PayloadSize}", message.Command, contextData.PayloadLength);

         if (!(message is UnknownMessage))
         {
            await this.networkMessageProcessorFactory.ProcessMessageAsync(message, peerContext, cancellation).ConfigureAwait(false);
            this.eventBus.Publish(new PeerMessageReceived(peerContext, message, contextData.GetTotalMessageLength()));
         }
      }
   }
}