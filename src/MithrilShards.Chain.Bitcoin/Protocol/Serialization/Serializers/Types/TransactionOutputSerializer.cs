using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class TransactionOutputSerializer : IProtocolTypeSerializer<TransactionOutput>
   {
      public TransactionOutput Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new TransactionOutput
         {
            Value = reader.ReadLong(),
            PublicKeyScript = reader.ReadByteArray()
         };
      }

      public int Serialize(TransactionOutput typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         int size = writer.WriteLong(typeInstance.Value);
         size += writer.WriteByteArray(typeInstance.PublicKeyScript);

         return size;
      }
   }
}
