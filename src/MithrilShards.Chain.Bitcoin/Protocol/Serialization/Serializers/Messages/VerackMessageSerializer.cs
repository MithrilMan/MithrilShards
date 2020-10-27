using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class VerackMessageSerializer : BitcoinNetworkMessageSerializerBase<VerackMessage>
   {
      public VerackMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      private static readonly VerackMessage instance = new VerackMessage();
      public override VerackMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         // having a singleton verack is fine because it contains no data
         return instance;
      }

      public override void Serialize(VerackMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         //NOP
      }
   }
}
