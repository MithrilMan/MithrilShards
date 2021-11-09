using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Example.Protocol.Types;

namespace MithrilShards.Example.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class PongMessage : INetworkMessage
{
   private const string COMMAND = "pong";
   string INetworkMessage.Command => COMMAND;

   public PongFancyResponse? PongFancyResponse { get; set; }
}
