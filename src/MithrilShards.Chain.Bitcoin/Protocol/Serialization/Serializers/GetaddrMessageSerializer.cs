using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class GetaddrMessageSerializer : NetworkMessageSerializerBase<GetaddrMessage> {

      public GetaddrMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      private static readonly GetaddrMessage instance = new GetaddrMessage();
      public override GetaddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) {
         return instance;
      }

      public override void Serialize(GetaddrMessage message, int protocolVersion, IBufferWriter<byte> output) {
         //NOP
      }
   }
}
