using System.Collections.Generic;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.P2P.Network.Server {
   public class ForgeServerSettings : MithrilShardSettingsBase {
      const int maxInboundConnectionsDefault = 20;

      public int MaxInboundConnections { get; set; }

      public List<ServerPeerBinding> Bindings { get; set; }

      public ForgeServerSettings() {
         this.MaxInboundConnections = maxInboundConnectionsDefault;
      }
   }
}
