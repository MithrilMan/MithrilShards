using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

[NetworkMessage(COMMAND)]
public sealed class GetDataMessage : INetworkMessage
{
   private const string COMMAND = "getdata";
   string INetworkMessage.Command => COMMAND;

   /// <summary>
   /// Inventory vectors
   /// </summary>
   public InventoryVector[]? Inventory { get; set; }
}
