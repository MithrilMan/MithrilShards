using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class TransactionWitnessSerializer : IProtocolTypeSerializer<TransactionWitness>
   {
      readonly IProtocolTypeSerializer<TransactionWitnessComponent> transactionWitnessComponentSerializer;

      public TransactionWitnessSerializer(IProtocolTypeSerializer<TransactionWitnessComponent> transactionWitnessComponentSerializer)
      {
         this.transactionWitnessComponentSerializer = transactionWitnessComponentSerializer;
      }

      public TransactionWitness Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new TransactionWitness
         {
            Components = reader.ReadArray(protocolVersion, this.transactionWitnessComponentSerializer)
         };
      }

      public int Serialize(TransactionWitness typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         return writer.WriteArray(typeInstance.Components, protocolVersion, this.transactionWitnessComponentSerializer);
      }
   }
}