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
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Network.Bedrock
{
   public class MithrilForgeClientConnectionHandler : ConnectionHandler
   {
      private readonly ILogger logger;
      private readonly IServiceProvider serviceProvider;
      private readonly IEventBus eventBus;
      private readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      private readonly IPeerContextFactory peerContextFactory;

      public MithrilForgeClientConnectionHandler(ILogger<MithrilForgeClientConnectionHandler> logger,
                                                 IServiceProvider serviceProvider,
                                                 IEventBus eventBus,
                                                 INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                                 IPeerContextFactory peerContextFactory)
      {
         this.logger = logger;
         this.serviceProvider = serviceProvider;
         this.eventBus = eventBus;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.peerContextFactory = peerContextFactory;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection)
      {
         // TODO: we could register processors as Scoped per connection and create a scope here
         //using var serviceProviderScope = serviceProvider.CreateScope();

         if (connection is null)
         {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable logScope = this.logger.BeginScope("Peer {PeerId} connected to outbound {PeerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);

         ProtocolReader reader = connection.CreateReader();
         INetworkProtocolMessageSerializer protocol = serviceProvider.GetRequiredService<INetworkProtocolMessageSerializer>();

         using IPeerContext peerContext = this.peerContextFactory.Create(PeerConnectionDirection.Outbound,
                                                 connection.ConnectionId,
                                                 connection.LocalEndPoint,
                                                 connection.RemoteEndPoint,
                                                 new NetworkMessageWriter(protocol, connection.CreateWriter()));

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

               await this.ProcessMessage(result.Message, peerContext, connection.ConnectionClosed)
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

      private async Task ProcessMessage(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
      {
         using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", message.Command);

         if (!(message is UnknownMessage))
         {
            await this.networkMessageProcessorFactory.ProcessMessageAsync(message, peerContext, cancellation).ConfigureAwait(false);
            this.eventBus.Publish(new PeerMessageReceived(peerContext, message));
         }
      }
   }
}