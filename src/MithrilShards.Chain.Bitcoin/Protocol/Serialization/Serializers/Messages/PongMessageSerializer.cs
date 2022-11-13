using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public sealed class PongMessageSerializer : BitcoinNetworkMessageSerializerBase<PongMessage>
{
   public override void Serialize(PongMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      output.WriteULong(message.Nonce);
   }

   public override PongMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      return new PongMessage { Nonce = reader.ReadULong() };
   }
}
