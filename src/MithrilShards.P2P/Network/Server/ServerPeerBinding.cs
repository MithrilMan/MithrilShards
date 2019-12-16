using System.Net;
using MithrilShards.P2P.Helpers;

namespace MithrilShards.P2P.Network.Server {
   /// <summary>
   /// Server Peer endpoint the node is listening to.
   /// </summary>
   public class ServerPeerBinding {
      /// <summary>IP address and port number on which the node server listens.</summary>
      public IPEndPoint Endpoint { get; set; }

      /// <summary>
      /// If <c>true</c>, peers that connect to this interface are white-listed.
      /// </summary>
      public bool IsWhitelistingEndpoint { get; set; }

      /// <summary>
      /// Initializes an instance of the object.
      /// </summary>
      /// <param name="endpoint">IP and port on which the node server listens.</param>
      /// <param name="isWhitelistingEndpoint">If <c>true</c>, peers that connect to this interface are white-listed.</param>
      public ServerPeerBinding(IPEndPoint endpoint, bool isWhitelistingEndpoint) {
         this.Endpoint = endpoint;
         this.IsWhitelistingEndpoint = isWhitelistingEndpoint;
      }


      /// <summary>
      /// Returns if the passed <paramref name="otherEndpoint"/> matches the specified server binding interface.
      /// </summary>
      /// <param name="otherEndpoint">The endpoint to match to <see cref="Endpoint"/>.</param>
      /// <remarks>If the binding endpoint is any IP (0.0.0.0 IPV4 or [::] IPV6 address) just checks the port.</remarks>
      /// <returns></returns>
      public bool Matches(IPEndPoint otherEndpoint) {
         if (this.Endpoint.Address.IsAnyIP()) {
            return this.Endpoint.Port == otherEndpoint.Port;
         }
         else {
            return otherEndpoint.Equals(this.Endpoint);
         }
      }
   }
}