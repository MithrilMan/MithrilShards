using MithrilShards.Core.Network.Server;

namespace MithrilShards.Core.Network {
   public class ConnectivityPeerStats : IConnectivityPeerStats {
      public int ConnectedInboundPeersCount { get; }

      public int ConnectedOutboundPeersCount { get; }
   }
}
