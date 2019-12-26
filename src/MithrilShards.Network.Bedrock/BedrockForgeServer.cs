﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Network.Server.Guards;
using BF = Bedrock.Framework;

namespace MithrilShards.Network.Bedrock {
   public class BedrockForgeServer : IForgeServer {
      readonly ILogger<BedrockForgeServer> logger;
      private readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      private readonly IServiceProvider serviceProvider;
      private readonly ForgeServerSettings settings;
      private readonly List<BF.Server> serverPeers;

      public BedrockForgeServer(ILogger<BedrockForgeServer> logger,
                                IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                                IOptions<ForgeServerSettings> settings,
                                IServiceProvider serviceProvider) {
         this.logger = logger;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.serviceProvider = serviceProvider;
         this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
         this.serverPeers = new List<BF.Server>();
      }

      public async Task InitializeAsync(CancellationToken cancellationToken) {
         this.CreateServerInstances();
      }

      public async Task StartAsync(CancellationToken cancellationToken) {
         foreach (BF.Server serverPeer in this.serverPeers) {
            serverPeer.StartAsync(cancellationToken);
         }
      }

      public async Task StopAsync(CancellationToken cancellationToken) {
         foreach (BF.Server serverPeer in this.serverPeers) {
            serverPeer.StopAsync();
         }
      }

      private void CreateServerInstances() {
         using (this.logger.BeginScope("CreateServerInstances")) {
            this.logger.LogInformation("Loading Forge Server listeners configuration.");

            if (this.serverPeerConnectionGuards.Any()) {
               this.logger.LogInformation(
                  "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
                  this.serverPeerConnectionGuards.Count(),
                  this.serverPeerConnectionGuards.Select(guard => guard.GetType().Name)
                  );
            }
            else {
               this.logger.LogWarning("No peer connection guards detected.");
            }

            if (this.settings.Bindings?.Count > 0) {
               this.logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", this.serverPeers.Count);

               Server server = new ServerBuilder(this.serviceProvider)
                  .UseSockets(sockets => {
                     foreach (ServerPeerBinding binding in this.settings.Bindings) {
                        if (!binding.IsValidEndpoint(out IPEndPoint parsedEndpoint)) {
                           throw new Exception($"Configuration error: binding {binding.Endpoint} must be a valid address:port value. Current value: {binding.Endpoint ?? "NULL"}");
                        }

                        if (binding.PublicEndpoint == null) {
                           binding.PublicEndpoint = new IPEndPoint(IPAddress.Loopback, parsedEndpoint.Port).ToString();
                        }

                        if (!binding.IsValidPublicEndpoint()) {
                           throw new Exception($"Configuration error: binding {nameof(binding.PublicEndpoint)} must be a valid address:port value. Current value: {binding.PublicEndpoint ?? "NULL"}");
                        }

                        var localEndPoint = IPEndPoint.Parse(binding.Endpoint);
                        var publicEndPoint = IPEndPoint.Parse(binding.PublicEndpoint);

                        this.logger.LogInformation("Added listener to local endpoint {ListenerLocalEndpoint}. (remote {ListenerPublicEndpoint})", localEndPoint, publicEndPoint);

                        sockets.Options.NoDelay = true;
                        sockets.Listen(
                           localEndPoint.Address,
                           localEndPoint.Port,
                           builder => builder.UseConnectionLogging().UseConnectionHandler<MithrilForgeConnectionHandler>()
                           );
                     }
                  }
               ).Build();

               this.serverPeers.Add(server);
            }
            else {
               this.logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
            }
         }
      }
   }
}
