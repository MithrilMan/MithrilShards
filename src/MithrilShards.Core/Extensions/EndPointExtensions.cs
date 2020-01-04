using System.Net;

namespace MithrilShards.Core.Extensions
{
   public static class EndPointExtensions
   {
      public static IPEndPoint AsIPEndPoint(this EndPoint endpoint)
      {
         return (IPEndPoint)endpoint;
      }
   }
}
