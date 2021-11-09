using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class PingMessage : INetworkMessage
{
   private const string COMMAND = "ping";
   string INetworkMessage.Command => COMMAND;

   /// <summary>
   /// A random nonce that identifies the ping request.
   /// </summary>
   public ulong Nonce { get; set; }
}
