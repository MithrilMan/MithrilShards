using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// The 'wtxidrelay' message is an empty message that a node sends to indicate
   /// that it prefers to receive wtxid-based transaction inventory announcements
   /// (using MSG_WTX type in inv/getdata) and that it supports relaying transactions
   /// by wtxid.
   /// Reference: BIP339
   /// </summary>
   [NetworkMessage(COMMAND)]
   public sealed class WtxidRelayMessage : INetworkMessage
   {
      public const string COMMAND = "wtxidrelay";
      string INetworkMessage.Command => COMMAND;

      // This message has no payload.
   }
}
