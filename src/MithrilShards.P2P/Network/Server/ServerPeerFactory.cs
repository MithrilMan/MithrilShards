using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MithrilShards.P2P.Network.Server {
   public class ServerPeerFactory : IServerPeerFactory {
      readonly ILogger<ServerPeerFactory> logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards;
      readonly IOptions<ForgeServerSettings> options;

      public ServerPeerFactory(ILogger<ServerPeerFactory> logger,
                               ILoggerFactory loggerFactory,
                               IEnumerable<IServerPeerConnectionGuard> serverPeerConnectionGuards,
                               IOptions<ForgeServerSettings> options) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.serverPeerConnectionGuards = serverPeerConnectionGuards;
         this.options = options;
      }

      public List<IServerPeer> CreateServerInstances() {
         var servers = new List<IServerPeer>();

         if (this.options.Value?.Bindings != null) {
            foreach (ServerPeerBinding binding in this.options.Value?.Bindings) {
               var serverPeer = new ServerPeer(
                  this.loggerFactory.CreateLogger<ServerPeer>(),
                  IPEndPoint.Parse(binding.Endpoint),
                  IPEndPoint.Parse(binding.Endpoint),
                  this.serverPeerConnectionGuards
                  );

               servers.Add(serverPeer);
            }
         }
         else {
            this.logger.LogWarning("No binding information found in configuration file, no Forge Servers available.");
         }

         return servers;
      }
   }
}
