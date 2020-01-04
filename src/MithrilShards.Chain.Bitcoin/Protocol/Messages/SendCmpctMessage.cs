using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   [NetworkMessage("sendcmpct")]
   public class SendCmpctMessage : NetworkMessage
   {

      /// <summary>
      /// A random nonce that identifies the ping request.
      /// </summary>
      public bool UseCmpctBlock { get; set; }

      public ulong Version { get; set; }

      public SendCmpctMessage() : base("sendcmpct")
      {
      }
   }
}