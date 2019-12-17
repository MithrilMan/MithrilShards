using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;

namespace MithrilShards.P2P.Network.Server {
   /// <summary>
   /// Ensures that during IBD, only white-listed nodes can pass.
   /// </summary>
   /// <seealso cref="ServerPeerConnectionGuardBase" />
   public class InitialBlockDownloadStateGuard : ServerPeerConnectionGuardBase {
      readonly IServerPeerSettings serverPeerSettings;
      readonly IInitialBlockDownloadState initialBlockDownloadState;

      public InitialBlockDownloadStateGuard(ILogger<InitialBlockDownloadStateGuard> logger, IServerPeerSettings serverPeerSettings, IInitialBlockDownloadState initialBlockDownloadState) : base(logger) {
         this.serverPeerSettings = serverPeerSettings;
         this.initialBlockDownloadState = initialBlockDownloadState;
      }

      internal override string TryGetDenyReason(TcpClient tcpClient) {
         if (this.initialBlockDownloadState.isInIBD) {
            var clientLocalEndPoint = tcpClient.Client.LocalEndPoint as IPEndPoint;
            var clientRemoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;

            bool clientIsWhiteListed = this.serverPeerSettings.Bindings
               .Any(binding => binding.IsWhitelistingEndpoint && binding.Matches(clientLocalEndPoint));

            if (!clientIsWhiteListed) {
               return "Node is in IBD and the peer is not white-listed.";
            }
         }

         return null;
      }
   }
}