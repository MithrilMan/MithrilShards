using System;
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

      public static bool IsAnyIP(this IPAddress address) {
         if (address.AddressFamily == AddressFamily.InterNetwork) {
            return address.Equals(IPAddress.Parse("0.0.0.0"));
         }
         else if (address.AddressFamily == AddressFamily.InterNetworkV6) {
            if (address.IsIPv4MappedToIPv6) {
               return address.Equals(IPAddress.Parse("0.0.0.0"));
            }
            else {
               return address.Equals(IPAddress.Parse("[::]"));
            }
         }
         else {
            throw new Exception("Unexpected address family");
         }
      }
   }
}
