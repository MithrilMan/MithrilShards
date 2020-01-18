using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Network.Client
{
   /// <summary>
   /// Tries to connect to peers configured to be connected.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Client.IConnector" />
   public class RequiredConnection : ConnectorBase
   {
      private const int INNER_DELAY = 500;
      private const int FAST_DELAY = 2_000;
      private const int MEDIUM_DELAY = 5_000;
      private const int SLOW_DELAY = 15_000;

      readonly IForgeConnectivity forgeConnectivity;
      public List<IPEndPoint> connectionsToAttempt = new List<IPEndPoint>();

      public RequiredConnection(ILogger<RequiredConnection> logger,
                                IEventBus eventBus,
                                IOptions<ForgeConnectivitySettings> options,
                                IConnectivityPeerStats serverPeerStats,
                                IForgeConnectivity forgeConnectivity) : base(logger, eventBus, options, serverPeerStats)
      {
         this.connectionsToAttempt.AddRange(
            from connection in this.settings.Connections
            let endPoint = connection.TryGetIPEndPoint(out IPEndPoint? endPoint) ? endPoint : null
            where endPoint != null
            select endPoint
            );
         this.forgeConnectivity = forgeConnectivity;
      }

      public override TimeSpan ComputeDelayAdjustment()
      {
         float fillRatioPercentage;
         //compute outbound slot fill ratio (0 - 1 range)
         if (this.peerStats.ConnectedOutboundPeersCount == 0)
         {
            fillRatioPercentage = 0;
         }
         else if (this.settings.MaxOutboundConnections == 0)
         {
            fillRatioPercentage = 0.5f;
         }
         else
         {
            fillRatioPercentage = this.settings.MaxOutboundConnections / this.peerStats.ConnectedOutboundPeersCount;
         }

         if (fillRatioPercentage < 0.25)
         {
            return TimeSpan.FromMilliseconds(FAST_DELAY);
         }
         else if (fillRatioPercentage < 0.7)
         {
            return TimeSpan.FromMilliseconds(MEDIUM_DELAY);
         }
         else
         {
            return TimeSpan.FromMilliseconds(SLOW_DELAY);
         }
      }


      protected override async ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation)
      {
         foreach (IPEndPoint endPoint in this.connectionsToAttempt)
         {
            if (cancellation.IsCancellationRequested) break;

            if (connectionManager.CanConnectTo(endPoint))
            {
               // note that AttemptConnection is not blocking because it returns when the peer fails to connect or when one of the parties disconnect
               _ = this.forgeConnectivity.AttemptConnectionAsync(endPoint, cancellation).ConfigureAwait(false);

               // apply a delay between attempts to prevent too many connection attempt in a row
               await Task.Delay(INNER_DELAY).ConfigureAwait(false);
            }
         }
      }
   }
}