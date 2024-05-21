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
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Network.Server.Guards;
using MithrilShards.Core.Shards;

namespace MithrilShards.Network.Bedrock;

public class BedrockNetworkShard(
   ILogger<BedrockNetworkShard> logger,
   IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
   IOptions<ForgeConnectivitySettings> settings,
   IServiceProvider serviceProvider
   ) : IMithrilShard
{
   private readonly ForgeConnectivitySettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
   private readonly List<Server> _serverPeers = [];

   public Task InitializeAsync(CancellationToken cancellationToken)
   {
      CreateServerInstances();
      return Task.CompletedTask;
   }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      foreach (Server serverPeer in _serverPeers)
      {
         _ = serverPeer.StartAsync(cancellationToken);
      }

      return Task.CompletedTask;
   }

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      var tasks = _serverPeers.Select(serverPeer => serverPeer.StopAsync(cancellationToken)).ToArray();

      try
      {
         await Task.WhenAll(tasks).ConfigureAwait(false);
      }
      catch (Exception e)
      {
         logger.LogError(e, "Error while stopping server peers.");
      }
   }

   private void CreateServerInstances()
   {
      using var _ = logger.BeginScope("CreateServerInstances");
      logger.LogInformation("Loading Forge Server listeners configuration.");

      if (serverPeerConnectionGuards.Any())
      {
         logger.LogInformation(
            "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
            serverPeerConnectionGuards.Count(),
            serverPeerConnectionGuards.Select(guard => guard.GetType().Name)
            );
      }
      else
      {
         logger.LogWarning("No peer connection guards detected.");
      }

      if (_settings.Listeners?.Count > 0)
      {
         logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", _serverPeers.Count);

         ServerBuilder builder = new ServerBuilder(serviceProvider)
            .UseSockets(sockets =>
            {
               //sockets.Options.NoDelay = true;

               foreach (ServerPeerBinding binding in _settings.Listeners)
               {
                  IPEndPoint localEndPoint = binding.GetIPEndPoint();

                  if (!binding.HasPublicEndPoint())
                  {
                     binding.PublicEndPoint = new IPEndPoint(IPAddress.Loopback, localEndPoint.Port).ToString();
                  }

                  binding.TryGetPublicIPEndPoint(out IPEndPoint? publicEndPoint);

                  logger.LogInformation("Added listener to local endpoint {ListenerLocalEndpoint}. (remote {ListenerPublicEndpoint})", localEndPoint, publicEndPoint);

                  sockets.Listen(
                     localEndPoint.Address,
                     localEndPoint.Port,
                     builder => builder
                        .UseConnectionLogging()
                        .UseConnectionHandler<MithrilForgeServerConnectionHandler>()
                     );

                  //var tcpListener = new TcpListener(localEndPoint.Address, localEndPoint.Port);
                  //tcpListener.Start();
                  //logger.LogInformation("Listening on {ListenerLocalEndpoint} for incoming connections.", tcpListener.LocalEndpoint);

               }
            }
         );

         builder.ShutdownTimeout = TimeSpan.FromSeconds(_settings.ForceShutdownAfter);

         Server server = builder.Build();

         _serverPeers.Add(server);
      }
      else
      {
         logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
      }
   }
}
