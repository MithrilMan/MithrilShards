using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Forge;

namespace MithrilShards.Network.Legacy.Server
{
   public class P2PForgeServer : IForgeConnectivity
   {
      readonly ILogger<P2PForgeServer> logger;
      readonly IServerPeerFactory serverPeerFactory;
      readonly List<IServerPeer> serverPeers;

      public P2PForgeServer(ILogger<P2PForgeServer> logger, IServerPeerFactory serverPeerFactory)
      {
         this.logger = logger;
         this.serverPeerFactory = serverPeerFactory;
         this.serverPeers = new List<IServerPeer>();
      }

      public ValueTask AttemptConnectionAsync(EndPoint remoteEndPoint, CancellationToken cancellation)
      {
         this.logger.LogWarning("AttemptConnection not implemented.");
         return default;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         this.serverPeers.AddRange(this.serverPeerFactory.CreateServerInstances());
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         foreach (IServerPeer serverPeer in this.serverPeers)
         {
            _ = serverPeer.ListenAsync(cancellationToken);
         }
         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         foreach (IServerPeer serverPeer in this.serverPeers)
         {
            serverPeer.StopListening();
         }
         return default;
      }
   }
}
