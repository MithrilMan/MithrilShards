using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class InvMessageSerializer : BitcoinNetworkMessageSerializerBase<InvMessage>
   {
      readonly IProtocolTypeSerializer<InventoryVector> _inventoryVectorSerializer;

      public InvMessageSerializer(IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer)
      {
         _inventoryVectorSerializer = inventoryVectorSerializer;
      }

      public override void Serialize(InvMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Inventory!, protocolVersion, _inventoryVectorSerializer);
      }

      public override InvMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         return new InvMessage { Inventory = reader.ReadArray(protocolVersion, _inventoryVectorSerializer) };
      }
   }
}
