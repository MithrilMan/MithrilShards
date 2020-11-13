using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class GetDataMessageSerializer : BitcoinNetworkMessageSerializerBase<GetDataMessage>
   {
      private static readonly GetDataMessage instance = new GetDataMessage();
      readonly IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer;

      public GetDataMessageSerializer(IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer)
      {
         this.inventoryVectorSerializer = inventoryVectorSerializer;
      }

      public override GetDataMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         return new GetDataMessage { Inventory = reader.ReadArray(protocolVersion, this.inventoryVectorSerializer) };
      }

      public override void Serialize(GetDataMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Inventory!, protocolVersion, this.inventoryVectorSerializer);
      }
   }
}
