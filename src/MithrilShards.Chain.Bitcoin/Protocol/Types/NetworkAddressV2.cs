using System.Net;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Represents a network address as described in BIP155 for the 'addrv2' message.
   /// This is distinct from NetworkAddress (used in 'addr' v1) and NetworkAddressNoTime.
   /// </summary>
   public class NetworkAddressV2
   {
      /// <summary>
      /// The time of the last great success after which the address was seen.
      /// If not known, this is the time of the address announcement.
      /// uint32, Bitcoin network messages use little-endian encoding.
      /// </summary>
      public uint Time { get; set; }

      /// <summary>
      /// The services offered by this peer (same as for 'version' message).
      /// ulong, serialized as CompactSize.
      /// </summary>
      public NodeServices Services { get; set; }

      /// <summary>
      /// The network identifier for the address that follows.
      /// byte, uses <see cref="BIP155NetworkId"/> enum.
      /// </summary>
      public BIP155NetworkId NetworkId { get; set; }

      // AddressLength is implicitly derived from NetworkId for known types or explicit for unknown types.
      // For serialization, it's read/written as a byte CompactSize.

      /// <summary>
      /// The network address. Length depends on NetworkId.
      /// Max 512 bytes.
      /// </summary>
      public byte[] Address { get; set; } = System.Array.Empty<byte>();

      /// <summary>
      /// The port number.
      /// uint16, big-endian.
      /// </summary>
      public ushort Port { get; set; }


      public NetworkAddressV2() { }

      /// <summary>
      /// Initializes a new instance of the <see cref="NetworkAddressV2"/> class from an IPEndPoint.
      /// This constructor is useful for converting known local or peer addresses to the new format.
      /// It assumes IPv4 or IPv6.
      /// </summary>
      /// <param name="ipEndPoint">The IP endpoint.</param>
      /// <param name="services">The services offered by this peer.</param>
      /// <param name="time">The time.</param>
      public NetworkAddressV2(IPEndPoint ipEndPoint, NodeServices services, uint time)
      {
         Time = time;
         Services = services;
         Port = (ushort)ipEndPoint.Port;

         if (ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
         {
            NetworkId = BIP155NetworkId.IPV4;
            Address = ipEndPoint.Address.GetAddressBytes();
         }
         else if (ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
         {
            NetworkId = BIP155NetworkId.IPV6;
            Address = ipEndPoint.Address.GetAddressBytes();
         }
         else
         {
            // Other address families (Tor, I2P, CJDNS) would need specific construction.
            // For simplicity, this constructor only handles IP.
            NetworkId = 0; // Unknown, would need custom handling
            Address = System.Array.Empty<byte>();
         }
      }

      /// <summary>
      /// Tries to get the length of the address based on the network ID.
      /// </summary>
      /// <param name="length">The length of the address if known, otherwise 0.</param>
      /// <returns>True if the network ID is known and has a fixed length, false otherwise.</returns>
      public bool TryGetFixedAddressLength(out byte length)
      {
         length = 0;
         switch (NetworkId)
         {
            case BIP155NetworkId.IPV4:
               length = 4;
               return true;
            case BIP155NetworkId.IPV6:
               length = 16;
               return true;
            case BIP155NetworkId.TORV2:
               length = 10;
               return true;
            case BIP155NetworkId.TORV3:
               length = 32;
               return true;
            case BIP155NetworkId.I2P:
               length = 32;
               return true;
            case BIP155NetworkId.CJDNS:
               length = 16;
               return true;
            default:
               return false;
         }
      }
   }
}
