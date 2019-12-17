using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;

namespace MithrilShards.P2P.Network.Server.Guards {
   public abstract class ServerPeerConnectionGuardBase : IServerPeerConnectionGuard {
      protected readonly ILogger logger;
      protected readonly ForgeServerSettings settings;

      public ServerPeerConnectionGuardBase(ILogger logger, IOptions<ForgeServerSettings> options) {
         this.logger = logger;
         this.settings = options.Value;
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