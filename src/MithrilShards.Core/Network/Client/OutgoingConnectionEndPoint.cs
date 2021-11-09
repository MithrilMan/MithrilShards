using System.Collections.Generic;
using System.Net;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Network.Client;

/// <summary>
/// Class used to pass information about the endpoint of the connection we want to establish plus additional items
/// that can be store in the <see cref="Items"/> dictionary.
/// These items can be read by the <see cref="IPeerContextFactory"/> implementation that can decide, for example, to
/// set some custom PeerContext field during the creation of the <see cref="IPeerContext"/> instance.
/// </summary>
public class OutgoingConnectionEndPoint
{
   /// <summary>
   /// Gets the end point of the remote peer.
   /// </summary>
   public IPEndPoint EndPoint { get; }

   /// <summary>
   /// Gets or sets a key/value collection that can be used to share data within the scope of connection.
   /// </summary>
   public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

   public OutgoingConnectionEndPoint(IPEndPoint endPoint)
   {
      EndPoint = endPoint.EnsureIPv6();
   }
}
