using System.ComponentModel.DataAnnotations;
using System.Net;
using MithrilShards.Core.Shards.Validation.ValidationAttributes;

namespace MithrilShards.Core.Network.Server;

/// <summary>
/// Client Peer endpoint the node would like to be connected to.
/// </summary>
public class ClientPeerBinding
{
   /// <summary>IP address and port number of the peer we wants to connect to.</summary>
   [IPEndPointValidator]
   [Required]
   public string EndPoint { get; set; } = string.Empty;

   public IPEndPoint GetIPEndPoint()
   {
      return IPEndPoint.Parse(EndPoint);
   }
}
