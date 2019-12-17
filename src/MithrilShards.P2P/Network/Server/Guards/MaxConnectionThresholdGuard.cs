using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MithrilShards.P2P.Network.Server.Guards {
   public class MaxConnectionThresholdGuard : ServerPeerConnectionGuardBase {
      readonly IServerPeerStats serverPeerStats;

      public MaxConnectionThresholdGuard(ILogger<InitialBlockDownloadStateGuard> logger,
                                         IOptions<ForgeServerSettings> settings,
                                         IServerPeerStats serverPeerStats) : base(logger, settings) {
         this.serverPeerStats = serverPeerStats;
      }

      internal override string TryGetDenyReason(TcpClient tcpClient) {
         if (this.serverPeerStats.ConnectedInboundPeersCount >= this.settings.MaxInboundConnections) {
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}