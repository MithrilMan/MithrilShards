using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class SendHeadersMessageSerializer : NetworkMessageSerializerBase<SendHeadersMessage>
   {
      private static readonly SendHeadersMessage instance = new SendHeadersMessage();

      public SendHeadersMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(SendHeadersMessage message, int protocolVersion, IBufferWriter<byte> output) { }

      public override SendHeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) => instance;
   }
}