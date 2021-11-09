using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class GetAddrMessageSerializer : BitcoinNetworkMessageSerializerBase<GetAddrMessage>
   {
      private static readonly GetAddrMessage _instance = new();

      public override GetAddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext) => _instance;

      public override void Serialize(GetAddrMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output) { }
   }
}
