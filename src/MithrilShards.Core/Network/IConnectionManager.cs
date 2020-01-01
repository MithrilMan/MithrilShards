using System.Net;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Network {
   public interface IConnectionManager : IHostedService, IConnectivityPeerStats {
      /// <summary>
      /// Gets the connected inbound peers count.
      /// </summary>
      /// <value>
      /// The connected inbound peers count.
      /// </value>
      int ConnectedInboundPeersCount { get; }

      /// <summary>
      /// Gets the connected outbound peers count.
      /// </summary>
      /// <value>
      /// The connected outbound peers count.
      /// </value>
      int ConnectedOutboundPeersCount { get; }

      /// <summary>
      /// Determines whether this Forge can connect to the specified end point.
      /// </summary>
      /// <param name="endPoint">The end point.</param>
      bool CanConnectTo(IPEndPoint endPoint);
   }
}
