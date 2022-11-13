using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public sealed class GetBlocksMessageSerializer : BitcoinNetworkMessageSerializerBase<GetBlocksMessage>
{
   private readonly IProtocolTypeSerializer<BlockLocator> _blockLocatorSerializer;
   private readonly IProtocolTypeSerializer<UInt256> _uint256Serializer;

   public GetBlocksMessageSerializer(IProtocolTypeSerializer<BlockLocator> blockLocatorSerializer, IProtocolTypeSerializer<UInt256> uint256Serializer)
   {
      _blockLocatorSerializer = blockLocatorSerializer;
      _uint256Serializer = uint256Serializer;
   }

   public override void Serialize(GetBlocksMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      output.WriteUInt(message.Version);
      output.WriteWithSerializer(message.BlockLocator!, protocolVersion, _blockLocatorSerializer);
      output.WriteWithSerializer(message.HashStop!, protocolVersion, _uint256Serializer);
   }

   public override GetBlocksMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      var message = new GetBlocksMessage
      {
         Version = reader.ReadUInt(),
         BlockLocator = reader.ReadWithSerializer(protocolVersion, _blockLocatorSerializer),
         HashStop = reader.ReadWithSerializer(protocolVersion, _uint256Serializer)
      };

      return message;
   }
}
