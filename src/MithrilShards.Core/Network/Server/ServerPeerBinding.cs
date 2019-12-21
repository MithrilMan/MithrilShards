using MithrilShards.Core.Extensions;
using System.Net;

namespace MithrilShards.Core.Network.Server {
   /// <summary>
   /// Server Peer endpoint the node is listening to.
   /// </summary>
   public class ServerPeerBinding {
      /// <summary>IP address and port number on which the node server listens.</summary>
      public string Endpoint { get; set; }

      /// <summary>External IP address and port number used to access the node from external network.</summary>
      /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
      public string PublicEndpoint { get; set; }

      /// <summary>
      /// If <c>true</c>, peers that connect to this interface are white-listed.
      /// </summary>
      public bool IsWhitelistingEndpoint { get; set; }

      /// <summary>
      /// Returns if the passed <paramref name="otherEndpoint"/> matches the specified server binding interface.
      /// </summary>
      /// <param name="otherEndpoint">The endpoint to match to <see cref="Endpoint"/>.</param>
      /// <remarks>If the binding endpoint is any IP (0.0.0.0 IPV4 or [::] IPV6 address) just checks the port.</remarks>
      /// <returns></returns>
      public bool Matches(IPEndPoint otherEndpoint) {
         IPEndPoint endpoint = IPEndPoint.Parse(this.Endpoint);
         if (endpoint.Address.IsAnyIP()) {
            return endpoint.Port == otherEndpoint.Port;
         }
         else {
            return otherEndpoint.Equals(this.Endpoint);
         }
      }

      public bool IsValidEndpoint(out IPEndPoint parsedEndpoint) {
         parsedEndpoint = null;
         return this.Endpoint != null && IPEndPoint.TryParse(this.Endpoint, out parsedEndpoint);
      }

      public bool IsValidPublicEndpoint() {
         return this.PublicEndpoint != null && IPEndPoint.TryParse(this.PublicEndpoint, out _);
      }
   }
}