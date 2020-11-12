using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards
{
   /// <summary>
   /// Guards against accepting connections from loopback addresses. (depend on <see cref="ForgeConnectivitySettings.AllowLoopbackConnection"/> configuration settings)
   /// </summary>
   /// <seealso cref="ServerPeerConnectionGuardBase" />
   public class ConnectToLoopbackGuard : ServerPeerConnectionGuardBase
   {

      public ConnectToLoopbackGuard(ILogger<ConnectToLoopbackGuard> logger, IOptions<ForgeConnectivitySettings> settings) : base(logger, settings)
      {
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (!this.settings.AllowLoopbackConnection && IPAddress.IsLoopback(peerContext.RemoteEndPoint.Address))
         {
            return $"Loopback peer connection not allowed (set {nameof(this.settings.AllowLoopbackConnection)} to true to allow such kind of connections).";
         }

         return null;
      }
   }
}