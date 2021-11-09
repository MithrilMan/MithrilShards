using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class PongMessage : INetworkMessage
{
   private const string COMMAND = "pong";
   string INetworkMessage.Command => COMMAND;

   /// <summary>
   /// The nonce received from the ping request.
   /// </summary>
   public ulong Nonce { get; set; }
}
