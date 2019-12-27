using System.Net;
using System.Net.Sockets;

namespace MithrilShards.Core.Extensions {
   public static class IPEndPointExtensions {
      public static IPEndPoint EnsureIPv6(this IPEndPoint endpoint) {
         if (endpoint.AddressFamily == AddressFamily.InterNetworkV6) {
            return endpoint;
         }

         return new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);
      }

      public static bool IsAnyIP(this IPEndPoint endPoint) {
         return endPoint.Address.IsAnyIP();
      }
   }
}
