using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class VerackMessage : INetworkMessage
{
   private const string COMMAND = "verack";
   string INetworkMessage.Command => COMMAND;
}
