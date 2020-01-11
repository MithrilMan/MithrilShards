using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Inventory vector (inv_vect).
   /// </summary>
   public class InventoryVector
   {
      public sealed class InventoryType
      {
         /// <summary>Any data of with this number may be ignored</summary>
         public const int ERROR = 0;

         /// <summary>Hash is related to a transaction</summary>
         public const int MSG_TX = 1;

         /// <summary>Hash is related to a data block</summary>
         public const int MSG_BLOCK = 2;

         /// <summary>
         /// Hash of a block header; identical to MSG_BLOCK. Only to be used in getdata message.
         /// Indicates the reply should be a merkleblock message rather than a block message; this only works if a bloom filter has been set.
         /// </summary>
         public const int MSG_FILTERED_BLOCK = 3;

         /// <summary>
         /// Hash of a block header; identical to MSG_BLOCK.
         /// Only to be used in getdata message.
         /// Indicates the reply should be a cmpctblock message. See BIP 152 for more info.
         /// </summary>
         public const int MSG_CMPCT_BLOCK = 4;
      }

      /// <summary>
      /// Identifies the object type linked to this inventory
      /// </summary>
      public uint Type { get; set; }

      public UInt256 Hash { get; set; }
   }
}
