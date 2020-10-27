using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class SendCmpctMessageSerializer : BitcoinNetworkMessageSerializerBase<SendCmpctMessage>
   {
      public SendCmpctMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(SendCmpctMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteBool(message.AnnounceUsingCompactBlock);
         output.WriteULong(message.Version);
      }

      public override SendCmpctMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         return new SendCmpctMessage { AnnounceUsingCompactBlock = reader.ReadBool(), Version = reader.ReadULong() };
      }
   }
}