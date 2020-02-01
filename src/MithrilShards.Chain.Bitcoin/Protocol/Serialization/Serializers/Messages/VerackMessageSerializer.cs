using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class VerackMessageSerializer : NetworkMessageSerializerBase<VerackMessage>
   {
      public VerackMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      private static readonly VerackMessage instance = new VerackMessage();
      public override VerackMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         // having a singleton verack is fine because it contains no data
         return instance;
      }

      public override void Serialize(VerackMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         //NOP
      }
   }
}
