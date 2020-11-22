using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MithrilShards.Example.Network.Client
{
   /// <summary>
   /// Client Peer endpoint the node would like to be connected to.
   /// </summary>
   public class ExampleClientPeerBinding
   {
      /// <summary>IP address and port number of the peer we wants to connect to.</summary>
      public string? EndPoint { get; set; }

      public string? AdditionalInformation { get; set; }

      public bool TryGetExampleEndPoint([MaybeNullWhen(false)] out ExampleEndPoint endPoint)
      {
         endPoint = null;

         if (!IPEndPoint.TryParse(EndPoint, out IPEndPoint ipEndPoint))
         {
            return false;
         }

         if (AdditionalInformation == null)
         {
            return false;
         }

         endPoint = new ExampleEndPoint(ipEndPoint.Address, ipEndPoint.Port, AdditionalInformation);
         return true;
      }
   }
}
