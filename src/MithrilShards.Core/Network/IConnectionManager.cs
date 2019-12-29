using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Network {
   public interface IConnectionManager : IHostedService {
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
   }
}
