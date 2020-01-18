using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class BlockHeaderSerializer : IProtocolTypeSerializer<BlockHeader>
   {
      private readonly IProtocolTypeSerializer<UInt256> uInt256Serializator;

      public BlockHeaderSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
      {
         this.uInt256Serializator = uInt256Serializator;
      }

      public BlockHeader Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new BlockHeader
         {
            Version = reader.ReadInt(),
            PreviousBlockHash = reader.ReadWithSerializer(protocolVersion, this.uInt256Serializator),
            MerkleRoot = reader.ReadWithSerializer(protocolVersion, this.uInt256Serializator),
            TimeStamp = reader.ReadUInt(),
            Bits = reader.ReadUInt(),
            Nonce = reader.ReadUInt(),
            TransactionCount = reader.ReadVarInt()
         };
      }

      public int Serialize(BlockHeader typeInstance, int protocolVersion, IBufferWriter<byte> writer)
      {
         int size = 0;
         size += writer.WriteInt(typeInstance.Version);
         size += writer.WriteWithSerializer(typeInstance.PreviousBlockHash!, protocolVersion, this.uInt256Serializator);
         size += writer.WriteWithSerializer(typeInstance.MerkleRoot!, protocolVersion, this.uInt256Serializator);
         size += writer.WriteUInt(typeInstance.TimeStamp);
         size += writer.WriteUInt(typeInstance.Bits);
         size += writer.WriteUInt(typeInstance.Nonce);
         size += writer.WriteVarInt(typeInstance.TransactionCount);

         return size;
      }
   }
}
