using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MithrilShards.P2P.Network.Server {
   public class ServerPeerFactory : IServerPeerFactory {
      readonly ILogger<ServerPeerFactory> logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly ForgeServerSettings settings;

      public ServerPeerFactory(ILogger<ServerPeerFactory> logger,
                               ILoggerFactory loggerFactory,
                               IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                               IOptions<ForgeServerSettings> settings
                               ) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.settings = settings.Value;
      }

      public List<IServerPeer> CreateServerInstances() {
         using (this.logger.BeginScope("CreateServerInstances")) {
            this.logger.LogInformation("Loading Forge Server listeners configuration.");
            var servers = new List<IServerPeer>();

            if (this.serverPeerConnectionGuards.Any()) {
               this.logger.LogInformation(
                  "Using {PeerConnectionGuardsCount} peer connection guards: {PeerConnectionGuards}.",
                  this.serverPeerConnectionGuards.Count(),
                  string.Join(',', this.serverPeerConnectionGuards.Select(guard => guard.GetType().Name))
                  );
            }
            else {
               this.logger.LogWarning("No peer connection guards detected.");
            }

            if (this.settings.Bindings != null) {

               foreach (ServerPeerBinding binding in this.settings.Bindings) {
                  if (!binding.IsValidEndpoint(out IPEndPoint parsedEndpoint)) {
                     throw new ArgumentException($"Configuration error: binding Endpoint must be a valid address:port value. Current value: {binding.Endpoint ?? "NULL"}", nameof(binding.Endpoint));
                  }

                  if (binding.PublicEndpoint == null) {
                     binding.PublicEndpoint = new IPEndPoint(IPAddress.Loopback, parsedEndpoint.Port).ToString();
                  }

                  if (!binding.IsValidPublicEndpoint()) {
                     throw new ArgumentException($"Configuration error: binding PublicEndpoint must be a valid address:port value. Current value: {binding.PublicEndpoint ?? "NULL"}", nameof(binding.PublicEndpoint));
                  }


                  var serverPeer = new ServerPeer(
                     this.loggerFactory.CreateLogger<ServerPeer>(),
                     IPEndPoint.Parse(binding.Endpoint),
                     IPEndPoint.Parse(binding.PublicEndpoint),
                     this.serverPeerConnectionGuards
                     );

                  servers.Add(serverPeer);
               }
            }

            if (servers.Count == 0) {
               this.logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
            }
            else {
               this.logger.LogInformation("Found {ConfiguredListeners} listeners in configuration.", servers.Count);
            }

            return servers;
         }
      }
   }
}
