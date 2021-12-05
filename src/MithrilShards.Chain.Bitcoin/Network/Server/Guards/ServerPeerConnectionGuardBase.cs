using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards;

public abstract partial class ServerPeerConnectionGuardBase : IServerPeerConnectionGuard
{
   protected readonly ForgeConnectivitySettings settings;

   public ServerPeerConnectionGuardBase(ILogger logger, IOptions<ForgeConnectivitySettings> options)
   {
      _logger = logger;
      settings = options.Value;
   }

   public ServerPeerConnectionGuardResult Check(IPeerContext peerContext)
   {
      string? denyReason = TryGetDenyReason(peerContext);
      if (!string.IsNullOrEmpty(denyReason))
      {
         DebugGuardNotPassed(denyReason);
         return ServerPeerConnectionGuardResult.Deny(denyReason);
      }

      return ServerPeerConnectionGuardResult.Allow();
   }

   internal abstract string? TryGetDenyReason(IPeerContext peerContext);
}

partial class ServerPeerConnectionGuardBase
{
   private readonly ILogger _logger;

   [LoggerMessage(0, LogLevel.Debug, "Peer connection guard not passed: {DenyReason}.")]
   partial void DebugGuardNotPassed(string denyReason);
}