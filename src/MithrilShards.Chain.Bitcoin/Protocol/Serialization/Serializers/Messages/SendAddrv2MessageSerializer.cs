using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class SendAddrv2MessageSerializer : INetworkMessageSerializer<SendAddrv2Message>
   {
      public void Deserialize(SendAddrv2Message message, IBitcoinReader reader)
      {
         // sendaddrv2 has no payload.
      }

      public void Serialize(SendAddrv2Message message, IBitcoinWriter writer)
      {
         // sendaddrv2 has no payload.
      }
   }
}
