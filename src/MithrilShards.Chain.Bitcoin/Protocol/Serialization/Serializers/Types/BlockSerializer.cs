using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;

public class BlockSerializer : IProtocolTypeSerializer<Block>
{
   private readonly IProtocolTypeSerializer<BlockHeader> _blockHeaderSerializer;
   readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;

   public BlockSerializer(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer, IProtocolTypeSerializer<Transaction> transactionSerializer)
   {
      _blockHeaderSerializer = blockHeaderSerializer;
      _transactionSerializer = transactionSerializer;
   }

   public Block Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
   {
      // Ensure options is not null and set necessary flags for transactions within a block
      var txOptions = (options ?? new ProtocolTypeSerializerOptions())
         .Set(SerializerOptions.HEADER_IN_BLOCK, false) // This option seems specific to header serialization context, not tx
         .Set(SerializerOptions.SERIALIZE_WITNESS, true); // CRITICAL: Enable witness parsing for transactions in a block

      return new Block
      {
         // Header options should not forcibly include SERIALIZE_WITNESS if it's not relevant for header itself
         Header = reader.ReadWithSerializer(protocolVersion, _blockHeaderSerializer, options ?? new ProtocolTypeSerializerOptions()),
         Transactions = reader.ReadArray(protocolVersion, _transactionSerializer, txOptions)
      };
   }

   public int Serialize(Block typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
   {
      // Ensure options is not null and set necessary flags for transactions within a block
      var txOptions = (options ?? new ProtocolTypeSerializerOptions())
         .Set(SerializerOptions.HEADER_IN_BLOCK, false) // This option seems specific to header serialization context, not tx
         .Set(SerializerOptions.SERIALIZE_WITNESS, true); // CRITICAL: Enable witness serialization for transactions in a block

      int size = 0;
      // Header options should not forcibly include SERIALIZE_WITNESS if it's not relevant for header itself
      size += writer.WriteWithSerializer(typeInstance.Header!, protocolVersion, _blockHeaderSerializer, options ?? new ProtocolTypeSerializerOptions());
      size += writer.WriteArray(typeInstance.Transactions!, protocolVersion, _transactionSerializer, txOptions);

      return size;
   }
}
