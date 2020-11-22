using System.Diagnostics.CodeAnalysis;
using System.Net;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Network.Server
{
   /// <summary>
   /// Server Peer endpoint the node is listening to.
   /// </summary>
   public class ServerPeerBinding
   {
      /// <summary>IP address and port number on which the node server listens.</summary>
      public string? EndPoint { get; set; }

      /// <summary>External IP address and port number used to access the node from external network.</summary>
      /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
      public string? PublicEndPoint { get; set; }

      /// <summary>
      /// If <c>true</c>, peers that connect to this interface are white-listed.
      /// </summary>
      public bool IsWhitelistingEndpoint { get; set; }

      /// <summary>
      /// Returns if the passed <paramref name="otherEndpoint"/> matches the specified server binding interface.
      /// </summary>
      /// <param name="otherEndpoint">The endpoint to match to <see cref="EndPoint"/>.</param>
      /// <remarks>If the binding endpoint is any IP (0.0.0.0 IPV4 or [::] IPV6 address) just checks the port.</remarks>
      /// <returns></returns>
      public bool Matches(IPEndPoint otherEndpoint)
      {
         var endpoint = IPEndPoint.Parse(EndPoint);
         if (endpoint.IsAnyIP())
         {
            return endpoint.Port == otherEndpoint.Port;
         }
         else
         {
            return otherEndpoint.Equals(EndPoint);
         }
      }

      public bool IsValidEndpoint([NotNullWhen(true)] out IPEndPoint? parsedEndpoint)
      {
         parsedEndpoint = null;
         return EndPoint != null && IPEndPoint.TryParse(EndPoint, out parsedEndpoint);
      }

      public bool IsValidPublicEndPoint()
      {
         return PublicEndPoint != null && IPEndPoint.TryParse(PublicEndPoint, out _);
      }

      public bool TryGetIPEndPoint(out IPEndPoint endPoint)
      {
         return IPEndPoint.TryParse(EndPoint, out endPoint);
      }

      public bool TryGetPublicIPEndPoint(out IPEndPoint publicEndPoint)
      {
         return IPEndPoint.TryParse(PublicEndPoint, out publicEndPoint);
      }
   }
}