using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MithrilShards.Core.Network.Server
{
   /// <summary>
   /// Client Peer endpoint the node would like to be connected to.
   /// </summary>
   public class ClientPeerBinding
   {
      /// <summary>IP address and port number of the peer we wants to connect to.</summary>
      public string? EndPoint { get; set; }

      public bool TryGetIPEndPoint([MaybeNullWhen(false)]out IPEndPoint endPoint)
      {
         return IPEndPoint.TryParse(EndPoint, out endPoint);
      }
   }
}