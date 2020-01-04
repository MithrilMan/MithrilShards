using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   [NetworkMessage("ping")]
   public class PingMessage : NetworkMessage
   {

      /// <summary>
      /// A random nonce that identifies the ping request.
      /// </summary>
      public ulong Nonce { get; set; }

      public PingMessage() : base("ping")
      {
      }
   }
}