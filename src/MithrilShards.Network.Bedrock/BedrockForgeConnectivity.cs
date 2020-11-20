using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Network.Bedrock
{
   public class BedrockForgeConnectivity : IForgeConnectivity
   {
      readonly ILogger<BedrockForgeConnectivity> logger;
      readonly IEventBus eventBus;
      private readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      private readonly IServiceProvider serviceProvider;
      readonly MithrilForgeClientConnectionHandler clientConnectionHandler;
      private readonly ForgeConnectivitySettings settings;
      private readonly List<Server> serverPeers;
      private Client client = null!;//initialized by InitializeAsync

      public BedrockForgeConnectivity(ILogger<BedrockForgeConnectivity> logger,
                                IEventBus eventBus,
                                IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                IOptions<ForgeConnectivitySettings> settings,
                                IServiceProvider serviceProvider,
                                MithrilForgeClientConnectionHandler clientConnectionHandler)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.serviceProvider = serviceProvider;
         this.clientConnectionHandler = clientConnectionHandler;
         this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
         this.serverPeers = new List<Server>();
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         this.CreateServerInstances();
         this.CreateClientBuilder();
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in this.serverPeers)
         {
            _ = serverPeer.StartAsync(cancellationToken);
         }

         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in this.serverPeers)
         {
            _ = serverPeer.StopAsync();
         }

         return default;
      }

      private void CreateServerInstances()
      {
         using (this.logger.BeginScope("CreateServerInstances"))
         {
            this.logger.LogInformation("Loading Forge Server listeners configuration.");

            if (this.serverPeerConnectionGuards.Any())
            {
               this.logger.LogInformation(
                  "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
                  this.serverPeerConnectionGuards.Count(),
                  this.serverPeerConnectionGuards.Select(guard => guard.GetType().Name)
                  );
            }
            else
            {
               this.logger.LogWarning("No peer connection guards detected.");
            }

            if (this.settings.Listeners?.Count > 0)
            {
               this.logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", this.serverPeers.Count);

               ServerBuilder builder = new ServerBuilder(this.serviceProvider)
                  .UseSockets(sockets =>
                  {
                     sockets.Options.NoDelay = true;

                     foreach (ServerPeerBinding binding in this.settings.Listeners)
                     {
                        if (!binding.IsValidEndpoint(out IPEndPoint? parsedEndpoint))
                        {
                           throw new Exception($"Configuration error: binding {binding.EndPoint} must be a valid address:port value. Current value: {binding.EndPoint ?? "NULL"}");
                        }

                        if (binding.PublicEndPoint == null)
                        {
                           binding.PublicEndPoint = new IPEndPoint(IPAddress.Loopback, parsedEndpoint.Port).ToString();
                        }

                        if (!binding.IsValidPublicEndPoint())
                        {
                           throw new Exception($"Configuration error: binding {nameof(binding.PublicEndPoint)} must be a valid address:port value. Current value: {binding.PublicEndPoint ?? "NULL"}");
                        }

                        var localEndPoint = IPEndPoint.Parse(binding.EndPoint);
                        var publicEndPoint = IPEndPoint.Parse(binding.PublicEndPoint);

                        this.logger.LogInformation("Added listener to local endpoint {ListenerLocalEndpoint}. (remote {ListenerPublicEndpoint})", localEndPoint, publicEndPoint);

                        sockets.Listen(
                           localEndPoint.Address,
                           localEndPoint.Port,
                           builder => builder
                              .UseConnectionLogging()
                              .UseConnectionHandler<MithrilForgeServerConnectionHandler>()
                           );
                     }
                  }
               );

               builder.ShutdownTimeout = TimeSpan.FromSeconds(this.settings.ForceShutdownAfter);

               Server server = builder.Build();

               this.serverPeers.Add(server);
            }
            else
            {
               this.logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
            }
         }
      }

      private void CreateClientBuilder()
      {
         this.client = new ClientBuilder(this.serviceProvider)
                                    .UseSockets()
                                    .UseConnectionLogging()
                                    .Build();
      }

      public async ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation)
      {
         using IDisposable logScope = this.logger.BeginScope("Outbound connection to {RemoteEndPoint}", remoteEndPoint);
         this.eventBus.Publish(new PeerConnectionAttempt(remoteEndPoint.EndPoint.AsIPEndPoint()));
         this.logger.LogDebug("Connection attempt to {RemoteEndPoint}", remoteEndPoint.EndPoint);
         try
         {
            await using ConnectionContext connection = await this.client.ConnectAsync(remoteEndPoint.EndPoint).ConfigureAwait(false);
            // we store the RemoteEndPoint class as a feature of the connection so we can then copy it into the PeerContext in the ClientConnectionHandler.OnConnectedAsync
            connection.Features.Set(remoteEndPoint);

            this.logger.LogDebug("Connected to {RemoteEndPoint}", connection.RemoteEndPoint);
            await this.clientConnectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            this.logger.LogDebug("Operation cancelled.");
         }
         catch (Exception ex)
         {
            this.logger.LogDebug(ex, "Unexpected connection terminated because of {DisconnectionReason}.", ex.Message);
         }
      }
   }
}
