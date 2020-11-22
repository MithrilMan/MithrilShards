using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class TransactionWitnessSerializer : IProtocolTypeSerializer<TransactionWitness>
   {
      readonly IProtocolTypeSerializer<TransactionWitnessComponent> _transactionWitnessComponentSerializer;

      public TransactionWitnessSerializer(IProtocolTypeSerializer<TransactionWitnessComponent> transactionWitnessComponentSerializer)
      {
         _transactionWitnessComponentSerializer = transactionWitnessComponentSerializer;
      }

      public TransactionWitness Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         return new TransactionWitness
         {
            Components = reader.ReadArray(protocolVersion, _transactionWitnessComponentSerializer)
         };
      }

      public int Serialize(TransactionWitness typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         return writer.WriteArray(typeInstance.Components, protocolVersion, _transactionWitnessComponentSerializer);
      }
   }
}