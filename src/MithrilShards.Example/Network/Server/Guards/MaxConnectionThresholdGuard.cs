using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;

namespace MithrilShards.Example.Network.Server.Guards
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
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}