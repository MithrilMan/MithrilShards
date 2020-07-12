using System;
using System.Buffers;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class UInt256Serializer : IProtocolTypeSerializer<UInt256>
   {
      public UInt256 Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new UInt256(reader.ReadBytes(32));
      }

      public int Serialize(UInt256 typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         ReadOnlySpan<byte> span = typeInstance.GetBytes();
         writer.Write(span);
         return span.Length;
      }
   }
}
