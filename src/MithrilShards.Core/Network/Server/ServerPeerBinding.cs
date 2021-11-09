using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Shards.Validation.ValidationAttributes;

namespace MithrilShards.Core.Network.Server;

/// <summary>
/// Server Peer endpoint the node is listening to.
/// </summary>
public class ServerPeerBinding
{
   /// <summary>IP address and port number on which the node server listens.</summary>
   [IPEndPointValidator]
   [Required]
   public string EndPoint { get; set; } = string.Empty;

   /// <summary>External IP address and port number used to access the node from external network.</summary>
   /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
   [IPEndPointValidator]
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

   public IPEndPoint GetIPEndPoint()
   {
      return IPEndPoint.Parse(EndPoint);
   }

   public bool TryGetPublicIPEndPoint([MaybeNullWhen(false)] out IPEndPoint publicEndPoint)
   {
      return IPEndPoint.TryParse(PublicEndPoint ?? string.Empty, out publicEndPoint);
   }

   public bool HasPublicEndPoint()
   {
      return PublicEndPoint != null;
   }
}
