using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// See BIP 152
   /// </summary>
   /// <seealso cref="INetworkMessage" />
   [NetworkMessage(COMMAND)]
   public sealed class SendCmpctMessage : INetworkMessage
   {
      private const string COMMAND = "sendcmpct";
      string INetworkMessage.Command => COMMAND;

      public bool HighBandwidthMode { get; set; }

      public ulong Version { get; set; }
   }
}