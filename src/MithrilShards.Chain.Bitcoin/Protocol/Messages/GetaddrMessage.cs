using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   [NetworkMessage("getaddr")]
   public class GetaddrMessage : NetworkMessage {

      public GetaddrMessage() : base("getaddr") {
      }
   }
}