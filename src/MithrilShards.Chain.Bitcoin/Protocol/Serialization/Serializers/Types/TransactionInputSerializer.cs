using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class TransactionInputSerializer : IProtocolTypeSerializer<TransactionInput>
   {
      readonly IProtocolTypeSerializer<OutPoint> outPointSerializator;

      public TransactionInputSerializer(IProtocolTypeSerializer<OutPoint> outPointSerializator)
      {
         this.outPointSerializator = outPointSerializator;
      }

      public TransactionInput Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new TransactionInput
         {
            PreviousOutput = reader.ReadWithSerializer(protocolVersion, this.outPointSerializator),
            SignatureScript = reader.ReadByteArray(),
            Sequence = reader.ReadUInt()
         };
      }

      public int Serialize(TransactionInput typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         int size = writer.WriteWithSerializer(typeInstance.PreviousOutput!, protocolVersion, this.outPointSerializator);
         size += writer.WriteByteArray(typeInstance.SignatureScript);

         return size;
      }
   }
}
