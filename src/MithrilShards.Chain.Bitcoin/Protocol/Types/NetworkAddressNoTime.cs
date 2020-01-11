using System;
using System.Net;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Network address (net_addr).
   /// </summary>
   public class NetworkAddressNoTime
   {
      /// <summary>
      /// Same service(s) listed in version.
      /// </summary>
      public ulong Services { get; set; }

      /// <summary>
      /// IPv6 address. Network byte order. The original client only supported IPv4 and only read the last 4 bytes to get the IPv4 address.
      /// However, the IPv4 address is written into the message as a 16 byte IPv4-mapped IPv6 address
      /// (12 bytes 00 00 00 00 00 00 00 00 00 00 FF FF, followed by the 4 bytes of the IPv4 address).
      /// </summary>
      public byte[] IP { get; set; }

      /// <summary>
      /// Port number, network byte order.
      /// </summary>
      public ushort Port { get; set; }

      public IPEndPoint EndPoint
      {
         get { return new IPEndPoint(new IPAddress(this.IP), this.Port); }
         set
         {
            if (value == null)
            {
               throw new InvalidOperationException("Can't set 'AddressReceiver' to null.");
            }

            this.IP = value.Address.MapToIPv6().GetAddressBytes();
            this.Port = (ushort)value.Port;
         }
      }
   }
}