using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   /// <summary>
   /// Serializer for the <see cref="WtxidRelayMessage"/>.
   /// This message has no payload.
   /// </summary>
   public class WtxidRelayMessageSerializer : INetworkMessageSerializer<WtxidRelayMessage>
   {
      public void Deserialize(WtxidRelayMessage message, IBitcoinReader reader)
      {
         // WtxidRelayMessage has no payload.
      }

      public void Serialize(WtxidRelayMessage message, IBitcoinWriter writer)
      {
         // WtxidRelayMessage has no payload.
      }
   }
}
