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

namespace MithrilShards.Network.Bedrock;

public class MithrilForgeServerConnectionHandler(
   ILogger<MithrilForgeServerConnectionHandler> logger,
   IServiceProvider serviceProvider,
   IEventBus eventBus,
   IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
   INetworkMessageProcessorFactory networkMessageProcessorFactory,
   IPeerContextFactory peerContextFactory) : ConnectionHandler
{

   public override async Task OnConnectedAsync(ConnectionContext connection)
   {
      ArgumentNullException.ThrowIfNull(connection);

      using var serviceProviderScope = serviceProvider.CreateScope();
      using var _ = logger.BeginScope("Peer {PeerId} connected to server {ServerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);

      ProtocolReader reader = connection.CreateReader();
      var protocolSerializer = serviceProviderScope.ServiceProvider.GetRequiredService<INetworkProtocolMessageSerializer>();

      IPeerContext peerContext = peerContextFactory.CreateIncomingPeerContext(
         connection.ConnectionId,
         connection.LocalEndPoint!.AsIPEndPoint().EnsureIPv6(),
         connection.RemoteEndPoint!.AsIPEndPoint().EnsureIPv6(),
         new NetworkMessageWriter(protocolSerializer, connection.CreateWriter()));

      // will dispose peerContext when out of scope, see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable
      await using var asyncDisposablePeerContext = peerContext.ConfigureAwait(false);

      using CancellationTokenRegistration cancellationRegistration = peerContext.ConnectionCancellationTokenSource.Token.Register(() =>
      {
         connection.Abort(new ConnectionAbortedException("Requested by PeerContext"));
      });

      connection.Features.Set(peerContext);
      protocolSerializer.SetPeerContext(peerContext);

      if (await EnsurePeerCanConnectAsync(connection, peerContext).ConfigureAwait(false))
      {

         await eventBus.PublishAsync(new PeerConnected(peerContext)).ConfigureAwait(false);

         await networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);

         while (true)
         {
            try
            {
               ProtocolReadResult<INetworkMessage> result = await reader.ReadAsync(protocolSerializer).ConfigureAwait(false);

               if (result.IsCompleted)
               {
                  break;
               }

               await ProcessMessageAsync(result.Message, peerContext, connection.ConnectionClosed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
               logger.LogDebug(ex, "Unexpected connection terminated because of {DisconnectionReason}.", ex.Message);
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
   private async ValueTask<bool> EnsurePeerCanConnectAsync(ConnectionContext connection, IPeerContext peerContext)
   {
      if (serverPeerConnectionGuards == null) return false;

      ServerPeerConnectionGuardResult? result = (
         from guard in serverPeerConnectionGuards
         let guardResult = guard.Check(peerContext)
         where guardResult.IsDenied
         select guardResult
         )
         .DefaultIfEmpty(ServerPeerConnectionGuardResult.Success)
         .FirstOrDefault();

      if (result == null) return true; // no guards

      if (result.IsDenied)
      {
         logger.LogDebug("Connection from client '{ConnectingPeerEndPoint}' was rejected because of {ClientDisconnectedReason} and will be closed.", connection.RemoteEndPoint, result.DenyReason);
         connection.Abort(new ConnectionAbortedException(result.DenyReason));
         await eventBus.PublishAsync(new PeerConnectionRejected(peerContext, result.DenyReason)).ConfigureAwait(false);
         return false;
      }

      return true;
   }

   private async Task ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
   {
      using var _ = logger.BeginScope("Processing message '{Command}'", message.Command);

      if (message is not UnknownMessage)
      {
         await networkMessageProcessorFactory.ProcessMessageAsync(message, peerContext, cancellation).ConfigureAwait(false);
         await eventBus.PublishAsync(new PeerMessageReceived(peerContext, message), cancellation).ConfigureAwait(false);
      }
   }
}