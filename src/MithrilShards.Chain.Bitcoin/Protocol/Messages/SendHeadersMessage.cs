using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// Request for Direct headers announcement.
   /// Upon receipt of this message, the node is be permitted, but not required, to announce new blocks 
   /// by headers command(instead of inv command).
   /// This message is supported by the protocol version >= 70012 or Bitcoin Core version >= 0.12.0.
   /// See BIP 130 for more information.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Messages.NetworkMessage" />
   [NetworkMessage("sendheaders")]
   public class SendHeadersMessage : NetworkMessage
   {
      public SendHeadersMessage() : base("sendheaders") { }
   }
}