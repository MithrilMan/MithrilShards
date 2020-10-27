using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class PongMessageSerializer : BitcoinNetworkMessageSerializerBase<PongMessage>
   {
      public PongMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(PongMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteULong(message.Nonce);
      }

      public override PongMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         return new PongMessage { Nonce = reader.ReadULong() };
      }
   }
}