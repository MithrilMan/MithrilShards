using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers
{
   public class GetAddrMessageSerializer : NetworkMessageSerializerBase<GetAddrMessage>
   {
      private static readonly GetAddrMessage instance = new GetAddrMessage();

      public GetAddrMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override GetAddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) => instance;

      public override void Serialize(GetAddrMessage message, int protocolVersion, IBufferWriter<byte> output) { }
   }
}
