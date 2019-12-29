using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards {
   public class MaxConnectionThresholdGuard : ServerPeerConnectionGuardBase {
      readonly IServerPeerStats serverPeerStats;

      public MaxConnectionThresholdGuard(ILogger<InitialBlockDownloadStateGuard> logger,
                                         IOptions<ForgeConnectivitySettings> settings,
                                         IServerPeerStats serverPeerStats) : base(logger, settings) {
         this.serverPeerStats = serverPeerStats;
      }

      internal override string TryGetDenyReason(IPeerContext peerContext) {
         if (this.serverPeerStats.ConnectedInboundPeersCount >= this.settings.MaxInboundConnections) {
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}