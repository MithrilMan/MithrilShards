using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class AddrMessage : INetworkMessage
{
   private const string COMMAND = "addr";
   string INetworkMessage.Command => COMMAND;

   public NetworkAddress[]? Addresses { get; set; }
}
