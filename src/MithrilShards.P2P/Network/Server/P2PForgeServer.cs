using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Forge;

namespace MithrilShards.Network.Network.Server {
   public class P2PForgeServer : IForgeServer {
      readonly ILogger<P2PForgeServer> logger;
      readonly IServerPeerFactory serverPeerFactory;
      readonly List<IServerPeer> serverPeers;

      public P2PForgeServer(ILogger<P2PForgeServer> logger, IServerPeerFactory serverPeerFactory) {
         this.logger = logger;
         this.serverPeerFactory = serverPeerFactory;
         this.serverPeers = new List<IServerPeer>();
      }

      public async Task InitializeAsync(CancellationToken cancellationToken) {
         this.serverPeers.AddRange(this.serverPeerFactory.CreateServerInstances());
      }

      public async Task StartAsync(CancellationToken cancellationToken) {
         foreach (IServerPeer serverPeer in this.serverPeers) {
            _ = serverPeer.ListenAsync(cancellationToken);
         }
      }

      public async Task StopAsync(CancellationToken cancellationToken) {
         foreach (IServerPeer serverPeer in this.serverPeers) {
            serverPeer.StopListening();
         }
      }
   }
}
