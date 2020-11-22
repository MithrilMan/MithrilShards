using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Network.Bedrock
{
   public class MithrilForgeServerConnectionHandler : ConnectionHandler
   {
      private readonly ILogger _logger;
      private readonly IServiceProvider _serviceProvider;
      private readonly IEventBus _eventBus;
      private readonly IEnumerable<IServerPeerConnectionGuard> _serverPeerConnectionGuards;
      private readonly INetworkMessageProcessorFactory _networkMessageProcessorFactory;
      private readonly IPeerContextFactory _peerContextFactory;

      public MithrilForgeServerConnectionHandler(ILogger<MithrilForgeServerConnectionHandler> logger,
                                                 IServiceProvider serviceProvider,
                                                 IEventBus eventBus,
                                                 IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                                 INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                                 IPeerContextFactory peerContextFactory)
      {
         _logger = logger;
         _serviceProvider = serviceProvider;
         _eventBus = eventBus;
         _serverPeerConnectionGuards = serverPeerConnectionGuards;
         _networkMessageProcessorFactory = networkMessageProcessorFactory;
         _peerContextFactory = peerContextFactory;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection)
      {
         if (connection is null)
         {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable loggerScope = _logger.BeginScope("Peer {PeerId} connected to server {ServerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);

         ProtocolReader reader = connection.CreateReader();
         INetworkProtocolMessageSerializer protocol = _serviceProvider.GetRequiredService<INetworkProtocolMessageSerializer>();

         using IPeerContext peerContext = _peerContextFactory.CreateIncomingPeerContext(connection.ConnectionId,
                                                                                            connection.LocalEndPoint.AsIPEndPoint().EnsureIPv6(),
                                                                                            connection.RemoteEndPoint.AsIPEndPoint().EnsureIPv6(),
                                                                                            new NetworkMessageWriter(protocol, connection.CreateWriter()));

         using CancellationTokenRegistration cancellationRegistration = peerContext.ConnectionCancellationTokenSource.Token.Register(() =>
         {
            connection.Abort(new ConnectionAbortedException("Requested by PeerContext"));
         });

         connection.Features.Set(peerContext);
         protocol.SetPeerContext(peerContext);

         if (EnsurePeerCanConnect(connection, peerContext))
         {

            _eventBus.Publish(new PeerConnected(peerContext));

            await _networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);

            while (true)
            {
               try
               {
                  ReadResult<INetworkMessage> result = await reader.ReadAsync(protocol).ConfigureAwait(false);

                  if (result.IsCompleted)
                  {
                     break;
                  }

                  await ProcessMessageAsync(result.Message, peerContext, connection.ConnectionClosed).ConfigureAwait(false);
               }
               catch (Exception ex)
               {
                  _logger.LogDebug(ex, "Unexpected connection terminated because of {DisconnectionReason}.", ex.Message);
                  break;
               }
               finally
               {
                  reader.Advance();
               }
            }

            return;
         }
      }

      /// <summary>
      /// Check if the client is allowed to connect based on certain criteria.
      /// </summary>
      /// <returns>When criteria is met returns <c>true</c>, to allow connection.</returns>
      private bool EnsurePeerCanConnect(ConnectionContext connection, IPeerContext peerContext)
      {
         if (_serverPeerConnectionGuards == null)
         {
            return false;
         }

         ServerPeerConnectionGuardResult result = (
            from guard in _serverPeerConnectionGuards
            let guardResult = guard.Check(peerContext)
            where guardResult.IsDenied
            select guardResult
            )
            .DefaultIfEmpty(ServerPeerConnectionGuardResult.Success)
            .FirstOrDefault();

         if (result.IsDenied)
         {
            _logger.LogDebug("Connection from client '{ConnectingPeerEndPoint}' was rejected because of {ClientDisconnectedReason} and will be closed.", connection.RemoteEndPoint, result.DenyReason);
            connection.Abort(new ConnectionAbortedException(result.DenyReason));
            _eventBus.Publish(new PeerConnectionAttemptFailed(peerContext, result.DenyReason));
            return false;
         }

         return true;
      }

      private async Task ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
      {
         using IDisposable logScope = _logger.BeginScope("Processing message '{Command}'", message.Command);

         if (!(message is UnknownMessage))
         {
            await _networkMessageProcessorFactory.ProcessMessageAsync(message, peerContext, cancellation).ConfigureAwait(false);
            _eventBus.Publish(new PeerMessageReceived(peerContext, message));
         }
      }
   }
}
