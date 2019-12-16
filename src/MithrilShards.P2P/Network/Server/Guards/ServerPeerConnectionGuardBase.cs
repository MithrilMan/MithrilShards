using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;

namespace MithrilShards.P2P.Network.Server {
   public abstract class ServerPeerConnectionGuardBase : IServerPeerConnectionGuard {
      protected readonly ILogger logger;
      readonly ICoreServices coreServices;

      public ServerPeerConnectionGuardBase(ICoreServices coreServices) {
         this.coreServices = coreServices;
         this.logger = coreServices.CreateLoggerOf(this.GetType());
      }

      public ServerPeerConnectionGuardResult Check(TcpClient tcpClient) {
         string denyReason = this.TryGetDenyReason(tcpClient);
         if (!string.IsNullOrEmpty(denyReason)) {
            this.logger.LogDebug("Peer connection guard not passed: {denyReason}", denyReason);
            return ServerPeerConnectionGuardResult.Deny(denyReason);
         }

         return ServerPeerConnectionGuardResult.Allow();
      }

      internal abstract string TryGetDenyReason(TcpClient tcpClient);
   }
}