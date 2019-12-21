using MithrilShards.Core.Network.Server;

namespace MithrilShards.Core.Network.Server {
   public class ServerPeerStats : IServerPeerStats {
      public int ConnectedInboundPeersCount { get; }
   }
}
