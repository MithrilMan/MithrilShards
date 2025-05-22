using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class Addrv2MessageSerializer : INetworkMessageSerializer<Addrv2Message>
   {
      private const int MAX_ADDR_COUNT = 1000;
      private const int MAX_ADDR_SIZE = 512;

      public void Deserialize(Addrv2Message message, IBitcoinReader reader)
      {
         uint addressCount = reader.ReadCompactUInt();
         if (addressCount > MAX_ADDR_COUNT)
         {
            throw new ProtocolViolationException($"addrv2 message contains too many addresses: {addressCount}, max {MAX_ADDR_COUNT}");
         }

         message.Addresses = new List<NetworkAddressV2>((int)addressCount);
         for (int i = 0; i < addressCount; i++)
         {
            var addr = new NetworkAddressV2
            {
               Time = reader.ReadUInt(),
               Services = (NodeServices)reader.ReadCompactULong(),
               NetworkId = (BIP155NetworkId)reader.ReadByte()
            };

            byte addressLength = (byte)reader.ReadCompactUInt();

            if (addr.TryGetFixedAddressLength(out byte fixedLength))
            {
               if (addressLength != fixedLength)
               {
                  throw new ProtocolViolationException($"Incorrect address length for {addr.NetworkId}: expected {fixedLength}, got {addressLength}");
               }
            }
            else // Unknown network ID or variable length (though BIP155 doesn't define variable length ones yet)
            {
               if (addressLength > MAX_ADDR_SIZE) // Protect against excessively large addresses for unknown types
               {
                  throw new ProtocolViolationException($"Address length {addressLength} for network ID {addr.NetworkId} is too large (max {MAX_ADDR_SIZE}).");
               }
            }

            addr.Address = reader.ReadBytes(addressLength);
            addr.Port = BinaryPrimitives.ReadUInt16BigEndian(reader.ReadBytes(2));

            message.Addresses.Add(addr);
         }
      }

      public void Serialize(Addrv2Message message, IBitcoinWriter writer)
      {
         writer.WriteCompactInt(message.Addresses.Count);
         foreach (NetworkAddressV2 addr in message.Addresses)
         {
            writer.WriteUInt(addr.Time);
            writer.WriteCompactULong((ulong)addr.Services);
            writer.WriteByte((byte)addr.NetworkId);

            if (addr.TryGetFixedAddressLength(out byte fixedLength))
            {
               if (addr.Address.Length != fixedLength)
               {
                  throw new ArgumentException($"Address length for {addr.NetworkId} is {addr.Address.Length} but should be {fixedLength}");
               }
               writer.WriteCompactInt(addr.Address.Length);
               writer.WriteBytes(addr.Address);
            }
            else // Unknown network ID or variable length
            {
               if (addr.Address.Length > MAX_ADDR_SIZE)
               {
                  throw new ArgumentException($"Address for network ID {addr.NetworkId} is too long: {addr.Address.Length}, max {MAX_ADDR_SIZE}");
               }
               writer.WriteCompactInt(addr.Address.Length);
               writer.WriteBytes(addr.Address);
            }

            Span<byte> portBytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(portBytes, addr.Port);
            writer.WriteBytes(portBytes);
         }
      }
   }
}
