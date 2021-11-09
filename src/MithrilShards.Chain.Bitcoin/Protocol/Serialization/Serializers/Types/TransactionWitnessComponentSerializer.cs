using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;

public class TransactionWitnessComponentSerializer : IProtocolTypeSerializer<TransactionWitnessComponent>
{
   public TransactionWitnessComponent Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
   {
      return new TransactionWitnessComponent
      {
         RawData = reader.ReadByteArray()
      };
   }

   public int Serialize(TransactionWitnessComponent typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
   {
      return writer.WriteByteArray(typeInstance.RawData);
   }
}
