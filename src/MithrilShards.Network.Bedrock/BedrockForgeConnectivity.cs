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
      readonly ILogger<BedrockForgeConnectivity> _logger;
      readonly IEventBus _eventBus;
      private readonly IEnumerable<IServerPeerConnectionGuard> _serverPeerConnectionGuards;
      private readonly IServiceProvider _serviceProvider;
      readonly MithrilForgeClientConnectionHandler _clientConnectionHandler;
      private readonly ForgeConnectivitySettings _settings;
      private readonly List<Server> _serverPeers;
      private Client _client = null!;//initialized by InitializeAsync

      public BedrockForgeConnectivity(ILogger<BedrockForgeConnectivity> logger,
                                IEventBus eventBus,
                                IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                IOptions<ForgeConnectivitySettings> settings,
                                IServiceProvider serviceProvider,
                                MithrilForgeClientConnectionHandler clientConnectionHandler)
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._serverPeerConnectionGuards = serverPeerConnectionGuards;
         this._serviceProvider = serviceProvider;
         this._clientConnectionHandler = clientConnectionHandler;
         this._settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
         this._serverPeers = new List<Server>();
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         this.CreateServerInstances();
         this.CreateClientBuilder();
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in this._serverPeers)
         {
            _ = serverPeer.StartAsync(cancellationToken);
         }

         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in this._serverPeers)
         {
            _ = serverPeer.StopAsync();
         }

         return default;
      }

      private void CreateServerInstances()
      {
         using (this._logger.BeginScope("CreateServerInstances"))
         {
            this._logger.LogInformation("Loading Forge Server listeners configuration.");

            if (this._serverPeerConnectionGuards.Any())
            {
               this._logger.LogInformation(
                  "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
                  this._serverPeerConnectionGuards.Count(),
                  this._serverPeerConnectionGuards.Select(guard => guard.GetType().Name)
                  );
            }
            else
            {
               this._logger.LogWarning("No peer connection guards detected.");
            }

            if (this._settings.Listeners?.Count > 0)
            {
               this._logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", this._serverPeers.Count);

               ServerBuilder builder = new ServerBuilder(this._serviceProvider)
                  .UseSockets(sockets =>
                  {
                     sockets.Options.NoDelay = true;

                     foreach (ServerPeerBinding binding in this._settings.Listeners)
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

                        this._logger.LogInformation("Added listener to local endpoint {ListenerLocalEndpoint}. (remote {ListenerPublicEndpoint})", localEndPoint, publicEndPoint);

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

               builder.ShutdownTimeout = TimeSpan.FromSeconds(this._settings.ForceShutdownAfter);

               Server server = builder.Build();

               this._serverPeers.Add(server);
            }
            else
            {
               this._logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
            }
         }
      }

      private void CreateClientBuilder()
      {
         this._client = new ClientBuilder(this._serviceProvider)
                                    .UseSockets()
                                    .UseConnectionLogging()
                                    .Build();
      }

      public async ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation)
      {
         using IDisposable logScope = this._logger.BeginScope("Outbound connection to {RemoteEndPoint}", remoteEndPoint);
         this._eventBus.Publish(new PeerConnectionAttempt(remoteEndPoint.EndPoint.AsIPEndPoint()));
         this._logger.LogDebug("Connection attempt to {RemoteEndPoint}", remoteEndPoint.EndPoint);
         try
         {
            await using ConnectionContext connection = await this._client.ConnectAsync(remoteEndPoint.EndPoint).ConfigureAwait(false);
            // we store the RemoteEndPoint class as a feature of the connection so we can then copy it into the PeerContext in the ClientConnectionHandler.OnConnectedAsync
            connection.Features.Set(remoteEndPoint);

            this._logger.LogDebug("Connected to {RemoteEndPoint}", connection.RemoteEndPoint);
            await this._clientConnectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            this._logger.LogDebug("Operation cancelled.");
         }
         catch (Exception ex)
         {
            this._logger.LogDebug(ex, "Unexpected connection terminated because of {DisconnectionReason}.", ex.Message);
         }
      }
   }
}
