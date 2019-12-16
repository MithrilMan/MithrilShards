using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public interface IServerPeerStats {
      public int ConnectedInboundPeersCount { get; }
   }
}
