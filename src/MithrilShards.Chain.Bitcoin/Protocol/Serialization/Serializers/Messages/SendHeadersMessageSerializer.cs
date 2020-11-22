using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class SendHeadersMessageSerializer : BitcoinNetworkMessageSerializerBase<SendHeadersMessage>
   {
      private static readonly SendHeadersMessage _instance = new SendHeadersMessage();

      public override void Serialize(SendHeadersMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output) { }

      public override SendHeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext) => _instance;
   }
}