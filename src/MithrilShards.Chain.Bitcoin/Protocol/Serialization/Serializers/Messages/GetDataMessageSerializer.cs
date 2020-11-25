using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class GetDataMessageSerializer : BitcoinNetworkMessageSerializerBase<GetDataMessage>
   {
      readonly IProtocolTypeSerializer<InventoryVector> _inventoryVectorSerializer;

      public GetDataMessageSerializer(IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer)
      {
         _inventoryVectorSerializer = inventoryVectorSerializer;
      }

      public override GetDataMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         return new GetDataMessage { Inventory = reader.ReadArray(protocolVersion, _inventoryVectorSerializer) };
      }

      public override void Serialize(GetDataMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Inventory!, protocolVersion, _inventoryVectorSerializer);
      }
   }
}
