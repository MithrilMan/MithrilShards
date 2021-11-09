using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public class VerackMessageSerializer : BitcoinNetworkMessageSerializerBase<VerackMessage>
{
   private static readonly VerackMessage _instance = new();
   public override VerackMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      // having a singleton verack is fine because it contains no data
      return _instance;
   }

   public override void Serialize(VerackMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      //NOP
   }
}
