using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace MithrilShards.P2P.Network.Server {
   public class MaxConnectionThresholdGuard : ServerPeerConnectionGuardBase {
      readonly IServerPeerSettings serverPeerSettings;
      readonly IServerPeerStats serverPeerStats;

      public MaxConnectionThresholdGuard(ILogger<InitialBlockDownloadStateGuard> logger, IServerPeerSettings serverPeerSettings, IServerPeerStats serverPeerStats) : base(logger) {
         this.serverPeerSettings = serverPeerSettings;
         this.serverPeerStats = serverPeerStats;
      }

      internal override string TryGetDenyReason(TcpClient tcpClient) {
         if (this.serverPeerStats.ConnectedInboundPeersCount >= this.serverPeerSettings.MaxInboundConnections) {
            return "Inbound connection refused: max connection threshold reached.";
         }

         return null;
      }
   }
}