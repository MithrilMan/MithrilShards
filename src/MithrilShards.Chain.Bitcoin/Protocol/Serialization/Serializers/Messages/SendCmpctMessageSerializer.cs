using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class SendCmpctMessageSerializer : NetworkMessageSerializerBase<SendCmpctMessage>
   {
      public SendCmpctMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(SendCmpctMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteBool(message.HighBandwidthMode);
         output.WriteULong(message.Version);
      }

      public override SendCmpctMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new SendCmpctMessage { HighBandwidthMode = reader.ReadBool(), Version = reader.ReadULong() };
      }
   }
}