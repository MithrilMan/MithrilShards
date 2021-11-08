using System;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Network.Bedrock
{
   public class MithrilForgeClientConnectionHandler : ConnectionHandler
   {
      private readonly ILogger _logger;
      private readonly IServiceProvider _serviceProvider;
      private readonly IEventBus _eventBus;
      private readonly INetworkMessageProcessorFactory _networkMessageProcessorFactory;
      private readonly IPeerContextFactory _peerContextFactory;

      public MithrilForgeClientConnectionHandler(ILogger<MithrilForgeClientConnectionHandler> logger,
                                                 IServiceProvider serviceProvider,
                                                 IEventBus eventBus,
                                                 INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                                 IPeerContextFactory peerContextFactory)
      {
         _logger = logger;
         _serviceProvider = serviceProvider;
         _eventBus = eventBus;
         _networkMessageProcessorFactory = networkMessageProcessorFactory;
         _peerContextFactory = peerContextFactory;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection)
      {
         // TODO: we could register processors as Scoped per connection and create a scope here
         //using var serviceProviderScope = serviceProvider.CreateScope();

         if (connection is null)
         {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable logScope = _logger.BeginScope("Peer {PeerId} connected to outbound {PeerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);

         ProtocolReader reader = connection.CreateReader();
         INetworkProtocolMessageSerializer protocol = _serviceProvider.GetRequiredService<INetworkProtocolMessageSerializer>();

         IPeerContext peerContext = _peerContextFactory.CreateOutgoingPeerContext(connection.ConnectionId,
                                                                                            connection.LocalEndPoint!,
                                                                                            connection.Features.Get<OutgoingConnectionEndPoint>(),
                                                                                            new NetworkMessageWriter(protocol, connection.CreateWriter()));

         // will dispose peerContext when out of scope, see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable
         await using var asyncDisposablePeerContext = peerContext.ConfigureAwait(false);

         connection.ConnectionClosed = peerContext.ConnectionCancellationTokenSource.Token;
         connection.Features.Set(peerContext);

         protocol.SetPeerContext(peerContext);

         await _eventBus.PublishAsync(new PeerConnected(peerContext)).ConfigureAwait(false);


         await _networkMessageProcessorFactory.StartProcessorsAsync(peerContext).ConfigureAwait(false);


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

      private async Task ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
      {
         using IDisposable logScope = _logger.BeginScope("Processing message '{Command}'", message.Command);

         if (!(message is UnknownMessage))
         {
            await _networkMessageProcessorFactory.ProcessMessageAsync(message, peerContext, cancellation).ConfigureAwait(false);
            await _eventBus.PublishAsync(new PeerMessageReceived(peerContext, message), cancellation).ConfigureAwait(false);
         }
      }
   }
}