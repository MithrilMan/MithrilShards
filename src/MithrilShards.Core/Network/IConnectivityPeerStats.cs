namespace MithrilShards.Core.Network {
   public interface IConnectivityPeerStats {
      /// <summary>
      /// Gets the connected inbound peers count.
      /// </summary>
      /// <value>
      /// The connected inbound peers count.
      /// </value>
      int ConnectedInboundPeersCount { get; }

      /// <summary>
      /// Gets the connected outbound peers count.
      /// </summary>
      /// <value>
      /// The connected outbound peers count.
      /// </value>
      int ConnectedOutboundPeersCount { get; }
   }
}
