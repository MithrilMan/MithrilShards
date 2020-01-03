using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class GetaddrMessageSerializer : NetworkMessageSerializerBase<GetAddrMessage> {

      public GetaddrMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      private static readonly GetAddrMessage instance = new GetAddrMessage();
      public override GetAddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) {
         return instance;
      }

      public override void Serialize(GetAddrMessage message, int protocolVersion, IBufferWriter<byte> output) {
         //NOP
      }
   }
}
