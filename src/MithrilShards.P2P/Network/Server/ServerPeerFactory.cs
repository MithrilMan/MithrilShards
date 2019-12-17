using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

            if (this.settings.Bindings != null) {

               foreach (ServerPeerBinding binding in this.settings.Bindings) {
                  var serverPeer = new ServerPeer(
                     this.loggerFactory.CreateLogger<ServerPeer>(),
                     IPEndPoint.Parse(binding.Endpoint),
                     IPEndPoint.Parse(binding.Endpoint),
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
