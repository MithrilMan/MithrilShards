namespace MithrilShards.Core.Network {
   public interface IConnectivityPeerStats {
      public int ConnectedInboundPeersCount { get; }

      public int ConnectedOutboundPeersCount { get; }
   }
}
