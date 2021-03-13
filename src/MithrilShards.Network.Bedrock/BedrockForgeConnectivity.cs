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
         _logger = logger;
         _eventBus = eventBus;
         _serverPeerConnectionGuards = serverPeerConnectionGuards;
         _serviceProvider = serviceProvider;
         _clientConnectionHandler = clientConnectionHandler;
         _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
         _serverPeers = new List<Server>();
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         CreateServerInstances();
         CreateClientBuilder();
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in _serverPeers)
         {
            _ = serverPeer.StartAsync(cancellationToken);
         }

         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         foreach (Server serverPeer in _serverPeers)
         {
            _ = serverPeer.StopAsync();
         }

         return default;
      }

      private void CreateServerInstances()
      {
         using (_logger.BeginScope("CreateServerInstances"))
         {
            _logger.LogInformation("Loading Forge Server listeners configuration.");

            if (_serverPeerConnectionGuards.Any())
            {
               _logger.LogInformation(
                  "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
                  _serverPeerConnectionGuards.Count(),
                  _serverPeerConnectionGuards.Select(guard => guard.GetType().Name)
                  );
            }
            else
            {
               _logger.LogWarning("No peer connection guards detected.");
            }

            if (_settings.Listeners?.Count > 0)
            {
               _logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", _serverPeers.Count);

               ServerBuilder builder = new ServerBuilder(_serviceProvider)
                  .UseSockets(sockets =>
                  {
                     sockets.Options.NoDelay = true;

                     foreach (ServerPeerBinding binding in _settings.Listeners)
                     {
                        IPEndPoint localEndPoint = binding.GetIPEndPoint();

                        if (!binding.HasPublicEndPoint())
                        {
                           binding.PublicEndPoint = new IPEndPoint(IPAddress.Loopback, localEndPoint.Port).ToString();
                        }

                        binding.TryGetPublicIPEndPoint(out IPEndPoint? publicEndPoint);

                        _logger.LogInformation("Added listener to local endpoint {ListenerLocalEndpoint}. (remote {ListenerPublicEndpoint})", localEndPoint, publicEndPoint);

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

               builder.ShutdownTimeout = TimeSpan.FromSeconds(_settings.ForceShutdownAfter);

               Server server = builder.Build();

               _serverPeers.Add(server);
            }
            else
            {
               _logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
            }
         }
      }

      private void CreateClientBuilder()
      {
         _client = new ClientBuilder(_serviceProvider)
                                    .UseSockets()
                                    .UseConnectionLogging()
                                    .Build();
      }

      public async ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation)
      {
         using IDisposable logScope = _logger.BeginScope("Outbound connection to {RemoteEndPoint}", remoteEndPoint);
         _eventBus.Publish(new PeerConnectionAttempt(remoteEndPoint.EndPoint.AsIPEndPoint()));

         bool connectionEstablished = false;

         try
         {
            await using ConnectionContext connection = await _client.ConnectAsync(remoteEndPoint.EndPoint).ConfigureAwait(false);
            // we store the RemoteEndPoint class as a feature of the connection so we can then copy it into the PeerContext in the ClientConnectionHandler.OnConnectedAsync
            connection.Features.Set(remoteEndPoint);
            connectionEstablished = true;

            await _clientConnectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            _logger.LogDebug("Connection to {RemoteEndPoint} canceled", remoteEndPoint.EndPoint);
            _eventBus.Publish(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), "Operation canceled."));
         }
         catch (Exception ex)
         {
            if (!connectionEstablished)
            {
               _eventBus.Publish(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), ex.Message));
            }
            else
            {
               _logger.LogError(ex, "AttemptConnectionAsync failed");
               //throw;
            }
         }
      }
   }
}
