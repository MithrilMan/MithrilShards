﻿using System;
using System.Buffers;
using System.Net;
using System.Text.Json.Serialization;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types
{
   /// <summary>
   /// Network address (net_addr).
   /// </summary>
   public class NetworkAddress : ISerializableProtocolType
   {
      readonly bool skipTimeField;

      /// <summary>
      /// The Time (version >= 31402). Not present in version message.
      /// </summary>
      public DateTimeOffset Time { get; set; }

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

      [JsonIgnore]
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

      public NetworkAddress() : this(false) { }

      /// <summary>
      /// Initializes a new instance of the <see cref="NetworkAddress"/> class.
      /// </summary>
      /// <param name="skipTimeField">if set to <c>true</c> skips time field serialization/deserialization, used by <see cref="VersionMessage"/>.</param>
      public NetworkAddress(bool skipTimeField)
      {
         this.skipTimeField = skipTimeField;
      }

      public void Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         // https://bitcoin.org/en/developer-reference#version
         if (!this.skipTimeField && protocolVersion >= KnownVersion.V31402)
         {
            this.Time = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt());
         }
         this.Services = reader.ReadULong();
         this.IP = reader.ReadBytes(16).ToArray();
         this.Port = reader.ReadUShort();
      }

      public int Serialize(IBufferWriter<byte> writer, int protocolVersion)
      {
         int size = 0;
         // https://bitcoin.org/en/developer-reference#version
         if (!this.skipTimeField && protocolVersion >= KnownVersion.V31402)
         {
            size += writer.WriteUInt((uint)this.Time.ToUnixTimeSeconds());
         }
         size += writer.WriteULong(this.Services);
         size += writer.WriteBytes(this.IP);
         size += writer.WriteUShort(this.Port);

         return size;
      }
   }
}
