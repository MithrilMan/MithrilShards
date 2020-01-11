using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class HeadersMessageSerializer : NetworkMessageSerializerBase<HeadersMessage>
   {
      private readonly IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer;

      public HeadersMessageSerializer(IChainDefinition chainDefinition, IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer) : base(chainDefinition)
      {
         this.blockHeaderSerializer = blockHeaderSerializer;
      }

      public override void Serialize(HeadersMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Headers, protocolVersion, this.blockHeaderSerializer);
      }

      public override HeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new HeadersMessage { Headers = reader.ReadArray(protocolVersion, this.blockHeaderSerializer) };
      }
   }
}