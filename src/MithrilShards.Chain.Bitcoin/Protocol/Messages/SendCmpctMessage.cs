using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// See BIP 152
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Messages.NetworkMessage" />
   [NetworkMessage("sendcmpct")]
   public class SendCmpctMessage : NetworkMessage
   {
      public bool HighBandwidthMode { get; set; }

      public ulong Version { get; set; }

      public SendCmpctMessage() : base("sendcmpct") { }
   }
}