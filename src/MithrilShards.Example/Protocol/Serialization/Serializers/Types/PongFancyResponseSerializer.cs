using System.Buffers;
using MithrilShards.Example.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Example.Protocol.Serialization.Serializers.Types
{
   public class PongFancyResponseSerializer : IProtocolTypeSerializer<PongFancyResponse>
   {
      public int Serialize(PongFancyResponse typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {

         int size = 0;
         size += writer.WriteULong(typeInstance.Nonce);
         size += writer.WriteVarString(typeInstance.Quote);

         return size;
      }

      public PongFancyResponse Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new PongFancyResponse
         {
            Nonce = reader.ReadULong(),
            Quote = reader.ReadVarString()
         };
      }
   }
}