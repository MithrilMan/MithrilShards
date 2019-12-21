using System.Net;

namespace MithrilShards.Core.Network.Server.Guards {
   public interface IPeerContext {
      IPEndPoint LocalEndPoint { get; }
      IPEndPoint RemoteEndPoint { get; }
   }
}
