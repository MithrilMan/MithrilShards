using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class OutPointSerializer : IProtocolTypeSerializer<OutPoint>
   {
      readonly IProtocolTypeSerializer<UInt256> _uInt256Serializator;

      public OutPointSerializer(IProtocolTypeSerializer<UInt256> uInt256Serializator)
      {
         _uInt256Serializator = uInt256Serializator;
      }

      public OutPoint Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new OutPoint
         {
            Hash = reader.ReadWithSerializer(protocolVersion, _uInt256Serializator),
            Index = reader.ReadUInt()
         };
      }

      public int Serialize(OutPoint typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         int size = writer.WriteWithSerializer(typeInstance.Hash!, protocolVersion, _uInt256Serializator);
         size += writer.WriteUInt(typeInstance.Index);

         return size;
      }
   }
}
