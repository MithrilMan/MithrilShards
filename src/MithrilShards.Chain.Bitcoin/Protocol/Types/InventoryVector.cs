using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types;

/// <summary>
/// Inventory vector (inv_vect).
/// </summary>
public class InventoryVector
{
   /// <summary>
   /// Identifies the object type linked to this inventory
   /// </summary>
   public uint Type { get; set; }

   public UInt256 Hash { get; set; } = null!;
}
