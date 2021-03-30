using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Threading;

namespace MithrilShards.Core.Network.Client
{
   /// <summary>
   /// Tries to connect to peers configured to be connected.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Client.ConnectorBase" />
   public class RequiredConnection : ConnectorBase
   {
      private const int INNER_DELAY = 500;
      private const int FAST_DELAY = 2_000;
      private const int MEDIUM_DELAY = 5_000;
      private const int SLOW_DELAY = 15_000;

      private readonly ForgeConnectivitySettings _settings;

      public List<OutgoingConnectionEndPoint> connectionsToAttempt = new List<OutgoingConnectionEndPoint>();

      public RequiredConnection(ILogger<RequiredConnection> logger,
                                IEventBus eventBus,
                                IOptions<ForgeConnectivitySettings> options,
                                IConnectivityPeerStats serverPeerStats,
                                IForgeClientConnectivity forgeConnectivity,
                                IPeriodicWork connectionLoop) : base(logger, eventBus, serverPeerStats, forgeConnectivity, connectionLoop)
      {
         _settings = options.Value!;

         connectionsToAttempt.AddRange(
            from connection in _settings.Connections
            select new OutgoingConnectionEndPoint(connection.GetIPEndPoint())
            );
      }

      public override TimeSpan ComputeDelayAdjustment()
      {
         float fillRatioPercentage;
         //compute outbound slot fill ratio (0 - 1 range)
         if (peerStats.ConnectedOutboundPeersCount == 0)
         {
            fillRatioPercentage = 0;
         }
         else if (_settings.MaxOutboundConnections == 0)
         {
            fillRatioPercentage = 0.5f;
         }
         else
         {
            fillRatioPercentage = _settings.MaxOutboundConnections / peerStats.ConnectedOutboundPeersCount;
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
         foreach (OutgoingConnectionEndPoint remoteEndPoint in connectionsToAttempt)
         {
            if (cancellation.IsCancellationRequested) break;

            if (connectionManager.CanConnectTo(remoteEndPoint.EndPoint))
            {
               // note that AttemptConnection is not blocking because it returns when the peer fails to connect or when one of the parties disconnect
               _ = forgeConnectivity.AttemptConnectionAsync(remoteEndPoint, cancellation).ConfigureAwait(false);

               // apply a delay between attempts to prevent too many connection attempt in a row
               await Task.Delay(INNER_DELAY).ConfigureAwait(false);
            }
         }
      }

      /// <summary>
      /// Tries the add end point.
      ///
      /// </summary>
      /// <param name="endPoint">The end point.</param>
      /// <returns><see langword="true"/> if the endpoint has been added, <see langword="false"/> if the endpoint was already listed.</returns>
      public bool TryAddEndPoint(IPEndPoint endPoint)
      {
         endPoint = endPoint.EnsureIPv6();
         if (connectionsToAttempt.Exists(ip => ip.Equals(endPoint)))
         {
            logger.LogDebug("EndPoint {RemoteEndPoint} already in the list of connections attempt.", endPoint);
            return false;
         }
         else
         {
            connectionsToAttempt.Add(new OutgoingConnectionEndPoint(endPoint));
            logger.LogDebug("EndPoint {RemoteEndPoint} added to the list of connections attempt.", endPoint);
            return true;
         }
      }


      /// <summary>
      /// Tries the remove end point from the list of connection attempts.
      /// </summary>
      /// <param name="endPoint">The end point to remove.</param>
      /// <returns><see langword="true"/> if the endpoint has been removed, <see langword="false"/> if the endpoint has not been found.</returns>
      public bool TryRemoveEndPoint(IPEndPoint endPoint)
      {
         return connectionsToAttempt.RemoveAll(remoteEndPoint => remoteEndPoint.EndPoint == endPoint) > 0;
      }
   }
}