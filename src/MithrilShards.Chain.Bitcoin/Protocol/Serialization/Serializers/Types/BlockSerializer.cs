﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
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
         options = (options ?? new ProtocolTypeSerializerOptions())
            .Set(SerializerOptions.HEADER_IN_BLOCK, false);

         return new Block
         {
            Header = reader.ReadWithSerializer(protocolVersion, _blockHeaderSerializer, options),
            Transactions = reader.ReadArray(protocolVersion, _transactionSerializer, options)
         };
      }

      public int Serialize(Block typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         options = (options ?? new ProtocolTypeSerializerOptions())
            .Set(SerializerOptions.HEADER_IN_BLOCK, false);

         int size = 0;
         size += writer.WriteWithSerializer(typeInstance.Header!, protocolVersion, _blockHeaderSerializer, options);
         size += writer.WriteArray(typeInstance.Transactions!, protocolVersion, _transactionSerializer, options);

         return size;
      }
   }
}