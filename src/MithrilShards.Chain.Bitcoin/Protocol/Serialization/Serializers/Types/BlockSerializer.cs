using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class BlockSerializer : IProtocolTypeSerializer<Block>
   {
      private readonly IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer;
      readonly IProtocolTypeSerializer<Transaction> transactionSerializer;

      public BlockSerializer(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer, IProtocolTypeSerializer<Transaction> transactionSerializer)
      {
         this.blockHeaderSerializer = blockHeaderSerializer;
         this.transactionSerializer = transactionSerializer;
      }

      public Block Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         options = (options ?? new ProtocolTypeSerializerOptions())
            .Set(SerializerOptions.HEADER_IN_BLOCK, false);

         return new Block
         {
            Header = reader.ReadWithSerializer(protocolVersion, this.blockHeaderSerializer, options),
            Transactions = reader.ReadArray(protocolVersion, this.transactionSerializer, options)
         };
      }

      public int Serialize(Block typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         options = (options ?? new ProtocolTypeSerializerOptions())
            .Set(SerializerOptions.HEADER_IN_BLOCK, false);

         int size = 0;
         size += writer.WriteWithSerializer(typeInstance.Header!, protocolVersion, this.blockHeaderSerializer, options);
         size += writer.WriteArray(typeInstance.Transactions!, protocolVersion, this.transactionSerializer, options);

         return size;
      }
   }
}