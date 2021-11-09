using System.Net;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Network;

public interface IConnectionManager : IHostedService, IConnectivityPeerStats
{
   /// <summary>
   /// Determines whether this Forge can connect to the specified end point.
   /// </summary>
   /// <param name="remoteEndPoint">The remote end point we'd like to connect to.</param>
   bool CanConnectTo(IPEndPoint remoteEndPoint);
}
