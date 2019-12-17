using System.Collections.Generic;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.P2P.Network.Server {
   public class ForgeServerSettings : MithrilShardSettingsBase {
      const int maxInboundConnectionsDefault = 20;
      const int maxOutboundConnectionsDefault = 20;

      public int MaxInboundConnections { get; set; }

      public int MaxOutboundConnections { get; set; }

      public List<ServerPeerBinding> Bindings { get; set; }

      public ForgeServerSettings() {
         this.Bindings = new List<ServerPeerBinding>();
       
         this.MaxInboundConnections = maxInboundConnectionsDefault;
         this.MaxOutboundConnections = maxOutboundConnectionsDefault;
      }
   }
}
