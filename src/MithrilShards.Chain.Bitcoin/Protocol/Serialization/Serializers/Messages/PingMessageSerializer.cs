using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public sealed class PingMessageSerializer : BitcoinNetworkMessageSerializerBase<PingMessage>
{
   public override void Serialize(PingMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      if (protocolVersion < KnownVersion.V60001)
      {
         return;
      }

      output.WriteULong(message.Nonce);
   }

   public override PingMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      var message = new PingMessage();

      if (protocolVersion < KnownVersion.V60001)
      {
         return message;
      }

      message.Nonce = reader.ReadULong();

      return message;
   }
}
