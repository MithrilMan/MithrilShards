using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public interface IServerPeerSettings {
      public int MaxInboundConnections { get; }

      IList<ServerPeerBinding> Bindings { get; }
   }
}
