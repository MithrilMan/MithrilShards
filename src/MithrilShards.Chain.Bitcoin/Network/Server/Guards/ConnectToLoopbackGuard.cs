using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards {
   /// <summary>
   /// Guards against accepting connections from loopback addresses. (depend on <see cref="ForgeConnectivitySettings.AllowLoopbackConnection"/> configuration settings)
   /// </summary>
   /// <seealso cref="ServerPeerConnectionGuardBase" />
   public class ConnectToLoopbackGuard : ServerPeerConnectionGuardBase {

      public ConnectToLoopbackGuard(ILogger<InitialBlockDownloadStateGuard> logger,
                                            IOptions<ForgeConnectivitySettings> settings,
                                            IInitialBlockDownloadState initialBlockDownloadState
                                            ) : base(logger, settings) {
      }

      internal override string TryGetDenyReason(IPeerContext peerContext) {
         if (!this.settings.AllowLoopbackConnection && IPAddress.IsLoopback(peerContext.RemoteEndPoint.Address)) {
            return $"Loopback peer connection not allowed (set {nameof(this.settings.AllowLoopbackConnection)} to true to allow such kind of connections).";
         }

         return null;
      }
   }
}