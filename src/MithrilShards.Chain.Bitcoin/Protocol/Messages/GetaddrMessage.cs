using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   [NetworkMessage("getaddr")]
   public class GetAddrMessage : NetworkMessage {

      public GetAddrMessage() : base("getaddr") {
      }
   }
}