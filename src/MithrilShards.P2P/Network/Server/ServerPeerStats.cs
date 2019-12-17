using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public class ServerPeerStats : IServerPeerStats {
      public int ConnectedInboundPeersCount { get; }
   }
}
