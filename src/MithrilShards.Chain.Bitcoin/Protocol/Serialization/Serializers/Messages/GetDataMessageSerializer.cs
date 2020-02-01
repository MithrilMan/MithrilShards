using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class GetDataMessageSerializer : NetworkMessageSerializerBase<GetDataMessage>
   {
      private static readonly GetDataMessage instance = new GetDataMessage();
      readonly IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer;

      public GetDataMessageSerializer(INetworkDefinition chainDefinition, IProtocolTypeSerializer<InventoryVector> inventoryVectorSerializer) : base(chainDefinition) {
         this.inventoryVectorSerializer = inventoryVectorSerializer;
      }

      public override GetDataMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new GetDataMessage { Inventory = reader.ReadArray(protocolVersion, this.inventoryVectorSerializer) };
      }

      public override void Serialize(GetDataMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Inventory!, protocolVersion, this.inventoryVectorSerializer);
      }
   }
}
