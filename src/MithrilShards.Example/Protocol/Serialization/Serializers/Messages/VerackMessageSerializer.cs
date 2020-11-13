using System.Buffers;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;

namespace MithrilShards.Example.Protocol.Serialization.Serializers.Messages
{
   public class VerackMessageSerializer : ExampleNetworkMessageSerializerBase<VerackMessage>
   {
      public VerackMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      private static readonly VerackMessage instance = new VerackMessage();
      public override VerackMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ExamplePeerContext peerContext)
      {
         // having a singleton verack is fine because it contains no data
         return instance;
      }

      public override void Serialize(VerackMessage message, int protocolVersion, ExamplePeerContext peerContext, IBufferWriter<byte> output)
      {
         //NOP
      }
   }
}
