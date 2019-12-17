using System.Net;
using MithrilShards.P2P.Helpers;

namespace MithrilShards.P2P.Network.Server {
   /// <summary>
   /// Server Peer endpoint the node is listening to.
   /// </summary>
   public class ServerPeerBinding {
      private IPEndPoint endpoint;

      /// <summary>IP address and port number on which the node server listens.</summary>
      public string Endpoint { get => this.endpoint.ToString(); set => this.endpoint = IPEndPoint.Parse(value); }

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
         if (this.endpoint.Address.IsAnyIP()) {
            return this.endpoint.Port == otherEndpoint.Port;
         }
         else {
            return otherEndpoint.Equals(this.Endpoint);
         }
      }
   }
}