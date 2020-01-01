using System.Collections.Generic;
using MithrilShards.Core.MithrilShards;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Core.Network {
   public class ForgeConnectivitySettings : MithrilShardSettingsBase {
      const int maxInboundConnectionsDefault = 20;
      const int maxOutboundConnectionsDefault = 20;
      const bool allowLoopbackConnectionDefault = true;

      public int MaxInboundConnections { get; set; }

      public int MaxOutboundConnections { get; set; }

      public bool AllowLoopbackConnection { get; set; }

      public List<ServerPeerBinding> Listeners { get; }

      public List<ClientPeerBinding> Connections { get; }

      public ForgeConnectivitySettings() {
         this.Listeners = new List<ServerPeerBinding>();
         this.Connections = new List<ClientPeerBinding>();

         this.MaxInboundConnections = maxInboundConnectionsDefault;
         this.MaxOutboundConnections = maxOutboundConnectionsDefault;
         this.AllowLoopbackConnection = allowLoopbackConnectionDefault;
      }
   }
}
