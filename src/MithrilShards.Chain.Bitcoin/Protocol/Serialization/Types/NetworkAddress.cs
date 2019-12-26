using System;
using System.Buffers;
using System.Net;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Network address (net_addr).
   /// </summary>
   public class NetworkAddress : ISerializableProtocolType<NetworkAddress> {
      readonly bool skipTimeField;

      public string InternalName => "net_addr";
      public int Length => 30;

      /// <summary>
      /// The Time (version >= 31402). Not present in version message.
      /// </summary>
      public uint Time { get; set; }

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

      public IPEndPoint EndPoint {
         get { return new IPEndPoint(new IPAddress(this.IP), this.Port); }
         set {
            if (value == null) {
               throw new InvalidOperationException("Can't set 'AddressReceiver' to null.");
            }

            this.IP = value.Address.MapToIPv6().GetAddressBytes();
            this.Port = (ushort)value.Port;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="NetworkAddress"/> class.
      /// </summary>
      /// <param name="skipTimeField">if set to <c>true</c> skips time field serialization/deserialization, used by <see cref="VersionMessage"/>.</param>
      public NetworkAddress(bool skipTimeField) {
         this.skipTimeField = skipTimeField;
      }

      public void Deserialize(SequenceReader<byte> data) {
         // https://bitcoin.org/en/developer-reference#version
         if (!this.skipTimeField) {
            this.Time = data.ReadUInt(); //DateTimeOffset.FromUnixTimeSeconds(data.ReadUInt);
         }
         this.Services = data.ReadULong();
         this.IP = data.ReadBytes(16);
         this.Port = data.ReadUShort();
      }

      public byte[] Serialize() {
         throw new NotImplementedException();
      }
   }
}
