using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class PongMessageSerializer : NetworkMessageSerializerBase<PongMessage>
   {
      public PongMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(PongMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteULong(message.Nonce);
      }

      public override PongMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new PongMessage { Nonce = reader.ReadULong() };
      }
   }
}