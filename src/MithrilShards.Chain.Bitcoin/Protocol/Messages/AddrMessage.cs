using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   [NetworkMessage("addr")]
   public class AddrMessage : NetworkMessage
   {

      public NetworkAddress[] Addresses { get; set; }

      public AddrMessage() : base("addr")
      {
      }
   }
}