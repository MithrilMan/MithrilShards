using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class InvMessage : INetworkMessage
{
   private const string COMMAND = "inv";
   string INetworkMessage.Command => COMMAND;

   public InventoryVector[]? Inventory { get; set; }
}
