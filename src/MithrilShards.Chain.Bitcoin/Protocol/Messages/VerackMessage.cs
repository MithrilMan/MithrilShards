using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   [NetworkMessage("verack")]
   public class VerackMessage : NetworkMessage
   {

      public VerackMessage() : base("verack")
      {
      }
   }
}