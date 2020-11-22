using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards
{
   public class MaxConnectionThresholdGuard : ServerPeerConnectionGuardBase
   {
      readonly IConnectivityPeerStats _peerStats;

      public MaxConnectionThresholdGuard(ILogger<MaxConnectionThresholdGuard> logger,
                                         IOptions<ForgeConnectivitySettings> settings,
                                         IConnectivityPeerStats serverPeerStats) : base(logger, settings)
      {
         _peerStats = serverPeerStats;
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (_peerStats.ConnectedInboundPeersCount >= settings.MaxInboundConnections)
         {
            /// TODO: try to evict eventual bad connection to let a space for this connection
            /// ref: https://github.com/bitcoin/bitcoin/blob/e8e79958a7b2a0bf1b02adcce9f4d811eac37dfc/src/net.cpp#L995-L1003
            /// I'd consider another approach: accept a connection that exceed the maximum allowed connection, try to handshake
            /// and if successful then decide either if evict one connect and keep this, or drop the connection attempt
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}