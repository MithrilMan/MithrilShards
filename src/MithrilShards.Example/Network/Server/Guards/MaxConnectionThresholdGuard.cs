using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;

namespace MithrilShards.Example.Network.Server.Guards
{
   public class MaxConnectionThresholdGuard : ServerPeerConnectionGuardBase
   {
      readonly IConnectivityPeerStats peerStats;

      public MaxConnectionThresholdGuard(ILogger<MaxConnectionThresholdGuard> logger,
                                         IOptions<ForgeConnectivitySettings> settings,
                                         IConnectivityPeerStats serverPeerStats) : base(logger, settings)
      {
         this.peerStats = serverPeerStats;
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (this.peerStats.ConnectedInboundPeersCount >= this.settings.MaxInboundConnections)
         {
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}