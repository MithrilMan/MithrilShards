using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.P2P.Network.Server {
   public class ServerPeerFactory : IServerPeerFactory {
      readonly ILogger<ServerPeerFactory> logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly IPeerConnectionFactory peerConnectionFactory;
      readonly ForgeServerSettings settings;

      public ServerPeerFactory(ILogger<ServerPeerFactory> logger,
                               ILoggerFactory loggerFactory,
                               IEventBus eventBus,
                               IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                               IOptions<ForgeServerSettings> settings,
                               IPeerConnectionFactory peerConnectionFactory
                               ) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.peerConnectionFactory = peerConnectionFactory;
         this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
      }

      public List<IServerPeer> CreateServerInstances() {
         using (this.logger.BeginScope("CreateServerInstances")) {
            this.logger.LogInformation("Loading Forge Server listeners configuration.");
            var servers = new List<IServerPeer>();

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

            if (this.settings.Bindings != null) {

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


                  var serverPeer = new ServerPeer(
                     this.loggerFactory.CreateLogger<ServerPeer>(),
                     this.eventBus,
                     IPEndPoint.Parse(binding.Endpoint),
                     IPEndPoint.Parse(binding.PublicEndpoint),
                     this.serverPeerConnectionGuards,
                     this.peerConnectionFactory
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
