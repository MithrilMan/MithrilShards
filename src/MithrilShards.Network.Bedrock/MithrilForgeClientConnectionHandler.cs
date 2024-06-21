using System;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Network.Bedrock;

public class MithrilForgeClientConnectionHandler(
   ILogger<MithrilForgeClientConnectionHandler> logger,
   IServiceProvider serviceProvider,
   IEventBus eventBus,
   INetworkMessageProcessorFactory networkMessageProcessorFactory,
   IPeerContextFactory peerContextFactory
   ) : ConnectionHandler
{

   public override async Task OnConnectedAsync(ConnectionContext connection)
   {
      ArgumentNullException.ThrowIfNull(connection);

      OutgoingConnectionEndPoint outgoingConnectionEndPoint = connection.Features.Get<OutgoingConnectionEndPoint>() ?? throw new NullReferenceException($"Missing {nameof(OutgoingConnectionEndPoint)} feature.");

      using var serviceProviderScope = serviceProvider.CreateScope();
      using var _ = logger.BeginScope("Peer {PeerId} connected to outbound {PeerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);

      ProtocolReader reader = connection.CreateReader();
      INetworkProtocolMessageSerializer protocol = serviceProviderScope.ServiceProvider.GetRequiredService<INetworkProtocolMessageSerializer>();

      IPeerContext peerContext = peerContextFactory.CreateOutgoingPeerContext(connection.ConnectionId,
                                                                                         connection.LocalEndPoint!,
                                                                                         outgoingConnectionEndPoint,
                                                                                         new NetworkMessageWriter(protocol, connection.CreateWriter()));

      // will dispose peerContext when out of scope, see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable
      await using var asyncDisposablePeerContext = peerContext.ConfigureAwait(false);

      connection.ConnectionClosed = peerContext.ConnectionCancellationTokenSource.Token;
      connection.Features.Set(peerContext);

      protocol.SetPeerContext(peerContext);

      await eventBus.PublishAsync(new PeerConnected(peerContext)).ConfigureAwait(false);


      await networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);


      while (true)
      {
         if (connection.ConnectionClosed.IsCancellationRequested)
         {
            break;
         }

         try
         {
            ProtocolReadResult<INetworkMessage> result = await reader.ReadAsync(protocol, connection.ConnectionClosed).ConfigureAwait(false);

            if (result.IsCompleted)
            {
               break;
            }

            await ProcessMessageAsync(result.Message, peerContext, connection.ConnectionClosed)
               .WaitAsync(connection.ConnectionClosed)
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
