using System.Net;

namespace MithrilShards.Core.Network.Server.Guards {
   public class PeerContext : IPeerContext {
      public IPEndPoint LocalEndPoint { get; }
      public IPEndPoint RemoteEndPoint { get; }

      public PeerContext(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) {
         LocalEndPoint = localEndPoint;
         RemoteEndPoint = remoteEndPoint;
      }
   }
}
