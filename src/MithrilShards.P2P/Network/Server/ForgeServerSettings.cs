using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public class ForgeServerSettings {
      public int MaxInboundConnections { get; set; }

      public IList<ServerPeerBinding> Bindings { get; set; }
   }
}
