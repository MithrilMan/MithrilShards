using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// The 'sendaddrv2' message tells the receiving peer that the sender is willing to
   /// receive 'addrv2' messages (BIP155). It has no payload.
   /// </summary>
   [NetworkMessage(COMMAND)]
   public sealed class SendAddrv2Message : INetworkMessage
   {
      public const string COMMAND = "sendaddrv2";
      string INetworkMessage.Command => COMMAND;
   }
}
