using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Serialization.Serializers.Messages
{
   public class Addrv2MessageSerializerTests
   {
      private Addrv2MessageSerializer CreateSerializer()
      {
         return new Addrv2MessageSerializer();
      }

      private (Addrv2Message message, byte[] expectedBytes) PrepareAddrV2Message_IPV4()
      {
         var message = new Addrv2Message
         {
            Addresses = new List<NetworkAddressV2>
                {
                    new NetworkAddressV2
                    {
                        Time = 1678886400, // Unix timestamp for a date
                        Services = NodeServices.Network | NodeServices.Witness,
                        NetworkId = BIP155NetworkId.IPV4,
                        Address = new byte[] { 192, 168, 1, 100 },
                        Port = 8333
                    }
                }
         };

         // Expected byte representation (example, needs actual calculation):
         // Count (1) = 0x01
         // Time (1678886400 = 0x6410C800) = 00 C8 10 64 (little-endian)
         // Services (Network | Witness = 1 | 8 = 9 = 0x09) = 09 (CompactSize ulong)
         // NetworkId (IPV4 = 1) = 0x01
         // AddressLength (4) = 0x04 (CompactSize)
         // Address (192.168.1.100) = C0 A8 01 64
         // Port (8333 = 0x208D) = 20 8D (big-endian)
         var expectedBytes = new byte[]
         {
                0x01, // Count
                0x00, 0xC8, 0x10, 0x64, // Time
                0x09, // Services
                0x01, // NetworkId
                0x04, // AddressLength
                0xC0, 0xA8, 0x01, 0x64, // Address
                0x20, 0x8D // Port
         };
         return (message, expectedBytes);
      }

      private (Addrv2Message message, byte[] expectedBytes) PrepareAddrV2Message_IPV6_TORV3()
      {
         var message = new Addrv2Message
         {
            Addresses = new List<NetworkAddressV2>
                {
                    new NetworkAddressV2 // IPv6 example
                    {
                        Time = 1678886401,
                        Services = NodeServices.Network,
                        NetworkId = BIP155NetworkId.IPV6,
                        Address = Enumerable.Range(0, 16).Select(i => (byte)(i + 0xA0)).ToArray(), // A0, A1, ... AF, B0, ... B9
                        Port = 8334
                    },
                    new NetworkAddressV2 // TorV3 example
                    {
                        Time = 1678886402,
                        Services = NodeServices.Witness,
                        NetworkId = BIP155NetworkId.TORV3,
                        Address = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), // 00, 01, ... 1F
                        Port = 9050
                    }
                }
         };

         // Expected byte representation (example, needs actual calculation)
         var byteList = new List<byte> { 0x02 }; // Count

         // IPv6 entry
         byteList.AddRange(new byte[] { 0x01, 0xC8, 0x10, 0x64 }); // Time (1678886401)
         byteList.Add(0x01); // Services (Network = 1)
         byteList.Add(0x02); // NetworkId (IPV6 = 2)
         byteList.Add(0x10); // AddressLength (16)
         byteList.AddRange(Enumerable.Range(0, 16).Select(i => (byte)(i + 0xA0))); // Address
         byteList.AddRange(new byte[] { 0x20, 0x8E }); // Port (8334)

         // TorV3 entry
         byteList.AddRange(new byte[] { 0x02, 0xC8, 0x10, 0x64 }); // Time (1678886402)
         byteList.Add(0x08); // Services (Witness = 8)
         byteList.Add(0x04); // NetworkId (TORV3 = 4)
         byteList.Add(0x20); // AddressLength (32)
         byteList.AddRange(Enumerable.Range(0, 32).Select(i => (byte)i)); // Address
         byteList.AddRange(new byte[] { 0x23, 0x5A }); // Port (9050)

         return (message, byteList.ToArray());
      }


      [Fact]
      public void SerializeDeserialize_IPV4_Message_IsCorrect()
      {
         var (message, expectedBytes) = PrepareAddrV2Message_IPV4();
         var serializer = CreateSerializer();
         var bufferWriter = new ArrayBufferWriter<byte>();

         // Act Serialize
         serializer.Serialize(message, 0, bufferWriter);
         var serializedBytes = bufferWriter.WrittenSpan.ToArray();

         // Assert Serialize
         Assert.Equal(expectedBytes, serializedBytes);

         // Act Deserialize
         var deserializedMessage = new Addrv2Message();
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(serializedBytes));
         serializer.Deserialize(deserializedMessage, 0, ref reader);

         // Assert Deserialize
         Assert.Single(deserializedMessage.Addresses);
         var addrOut = deserializedMessage.Addresses[0];
         var addrIn = message.Addresses[0];
         Assert.Equal(addrIn.Time, addrOut.Time);
         Assert.Equal(addrIn.Services, addrOut.Services);
         Assert.Equal(addrIn.NetworkId, addrOut.NetworkId);
         Assert.Equal(addrIn.Address, addrOut.Address);
         Assert.Equal(addrIn.Port, addrOut.Port);
      }

      [Fact]
      public void SerializeDeserialize_IPV6_TORV3_Message_IsCorrect()
      {
         var (message, expectedBytes) = PrepareAddrV2Message_IPV6_TORV3();
         var serializer = CreateSerializer();
         var bufferWriter = new ArrayBufferWriter<byte>();

         // Act Serialize
         serializer.Serialize(message, 0, bufferWriter);
         var serializedBytes = bufferWriter.WrittenSpan.ToArray();

         // Assert Serialize
         Assert.Equal(expectedBytes, serializedBytes);

         // Act Deserialize
         var deserializedMessage = new Addrv2Message();
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(serializedBytes));
         serializer.Deserialize(deserializedMessage, 0, ref reader);

         // Assert Deserialize
         Assert.Equal(2, deserializedMessage.Addresses.Count);

         for (int i = 0; i < message.Addresses.Count; i++)
         {
            var addrOut = deserializedMessage.Addresses[i];
            var addrIn = message.Addresses[i];
            Assert.Equal(addrIn.Time, addrOut.Time);
            Assert.Equal(addrIn.Services, addrOut.Services);
            Assert.Equal(addrIn.NetworkId, addrOut.NetworkId);
            Assert.Equal(addrIn.Address, addrOut.Address);
            Assert.Equal(addrIn.Port, addrOut.Port);
         }
      }

      [Fact]
      public void Deserialize_TooManyAddresses_ThrowsProtocolViolationException()
      {
         var serializer = CreateSerializer();
         var buffer = new List<byte>();
         // Max addresses = 1000. Count = 1001 (0xFD 0xE9 0x03)
         buffer.AddRange(new byte[] { 0xFD, 0xE9, 0x03 }); // CompactSize for 1001

         // Add dummy data for one address to make the stream longer
         buffer.AddRange(new byte[] { 0x00, 0xC8, 0x10, 0x64, 0x09, 0x01, 0x04, 0xC0, 0xA8, 0x01, 0x64, 0x20, 0x8D });


         var sequence = new ReadOnlySequence<byte>(buffer.ToArray());
         var reader = new SequenceReader<byte>(sequence);
         var message = new Addrv2Message();

         Assert.Throws<ProtocolViolationException>(() => serializer.Deserialize(message, 0, ref reader));
      }

      [Fact]
      public void Deserialize_IncorrectAddressLength_ThrowsProtocolViolationException()
      {
         var serializer = CreateSerializer();
         var buffer = new List<byte>();
         buffer.Add(0x01); // Count = 1
         buffer.AddRange(new byte[] { 0x00, 0xC8, 0x10, 0x64 }); // Time
         buffer.Add(0x09); // Services
         buffer.Add(0x01); // NetworkId (IPV4)
         buffer.Add(0x05); // AddressLength (5 - incorrect for IPV4)
         buffer.AddRange(new byte[] { 0xC0, 0xA8, 0x01, 0x64, 0x00 }); // Address (5 bytes)
         buffer.AddRange(new byte[] { 0x20, 0x8D }); // Port

         var sequence = new ReadOnlySequence<byte>(buffer.ToArray());
         var reader = new SequenceReader<byte>(sequence);
         var message = new Addrv2Message();

         Assert.Throws<ProtocolViolationException>(() => serializer.Deserialize(message, 0, ref reader));
      }
   }
}
