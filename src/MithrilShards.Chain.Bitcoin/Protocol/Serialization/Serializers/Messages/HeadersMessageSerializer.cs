using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public class HeadersMessageSerializer : BitcoinNetworkMessageSerializerBase<HeadersMessage>
{
   private readonly IProtocolTypeSerializer<BlockHeader> _blockHeaderSerializer;

   public HeadersMessageSerializer(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer)
   {
      _blockHeaderSerializer = blockHeaderSerializer;
   }

   public override void Serialize(HeadersMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      output.WriteArray(message.Headers!, protocolVersion, _blockHeaderSerializer);
   }

   public override HeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      return new HeadersMessage { Headers = reader.ReadArray(protocolVersion, _blockHeaderSerializer) };
   }
}
