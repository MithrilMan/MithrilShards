using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards
{
   /// <summary>
   /// Ensures that during IBD, only white-listed nodes can pass.
   /// </summary>
   /// <seealso cref="ServerPeerConnectionGuardBase" />
   public class InitialBlockDownloadStateGuard : ServerPeerConnectionGuardBase
   {
      readonly IInitialBlockDownloadState initialBlockDownloadState;

      public InitialBlockDownloadStateGuard(ILogger<InitialBlockDownloadStateGuard> logger,
                                            IOptions<ForgeConnectivitySettings> settings,
                                            IInitialBlockDownloadState initialBlockDownloadState
                                            ) : base(logger, settings)
      {
         this.initialBlockDownloadState = initialBlockDownloadState;
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (this.initialBlockDownloadState.isInIBD)
         {

            bool clientIsWhiteListed = this.settings.Listeners
               .Any(binding => binding.IsWhitelistingEndpoint && binding.Matches(peerContext.LocalEndPoint));

            if (!clientIsWhiteListed)
            {
               return "Node is in IBD and the peer is not white-listed.";
            }
         }

         return null;
      }
   }
}