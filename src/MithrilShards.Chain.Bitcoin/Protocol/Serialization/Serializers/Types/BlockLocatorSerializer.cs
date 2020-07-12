using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class BlockLocatorSerializer : IProtocolTypeSerializer<BlockLocator>
   {
      private readonly IProtocolTypeSerializer<UInt256> uInt256Serializator;

      public BlockLocatorSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
      {
         this.uInt256Serializator = uInt256Serializator;
      }

      public BlockLocator Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new BlockLocator { BlockLocatorHashes = reader.ReadArray(protocolVersion, this.uInt256Serializator) };
      }

      public int Serialize(BlockLocator typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         return writer.WriteArray(typeInstance.BlockLocatorHashes, protocolVersion, this.uInt256Serializator);
      }
   }
}
