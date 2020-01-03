using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class HeadersMessageSerializer : NetworkMessageSerializerBase<HeadersMessage> {
      public HeadersMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(HeadersMessage message, int protocolVersion, IBufferWriter<byte> output) {
         output.WriteArray(message.Headers);
      }

      public override HeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) {
         return new HeadersMessage {
            Headers = reader.ReadArray<BlockHeader>()
         };
      }
   }
}