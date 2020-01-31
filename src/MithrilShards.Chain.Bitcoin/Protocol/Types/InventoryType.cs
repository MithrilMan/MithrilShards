namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   public sealed class InventoryType
   {
      public const uint MSG_WITNESS_FLAG = 1 << 30;
      public const uint MSG_TYPE_MASK = 0xffffffff >> 2;

      /// <summary>Any data of with this number may be ignored</summary>
      public const uint ERROR = 0;

      /// <summary>Hash is related to a transaction</summary>
      public const uint MSG_TX = 1;

      /// <summary>Hash is related to a data block</summary>
      public const uint MSG_BLOCK = 2;

      /// <summary>
      /// Hash of a block header; identical to MSG_BLOCK. Only to be used in getdata message.
      /// Indicates the reply should be a merkleblock message rather than a block message; this only works if a bloom filter has been set.
      /// </summary>
      public const uint MSG_FILTERED_BLOCK = 3;

      /// <summary>
      /// Hash of a block header; identical to MSG_BLOCK.
      /// Only to be used in getdata message.
      /// Indicates the reply should be a cmpctblock message. See BIP 152 for more info.
      /// </summary>
      public const uint MSG_CMPCT_BLOCK = 4;

      public const uint MSG_WITNESS_BLOCK = MSG_BLOCK | MSG_WITNESS_FLAG; // Defined in BIP144

      /// <summary>
      /// Hash of a witness transaction.
      /// Defined in BIP144
      /// </summary>
      public const uint MSG_WITNESS_TX = MSG_TX | MSG_WITNESS_FLAG;

      /// <summary>
      /// Hash of a block header; identical to MSG_FILTERED_BLOCK. Only to be used with witness blocks.
      /// </summary>
      public const uint MSG_FILTERED_WITNESS_BLOCK = MSG_FILTERED_BLOCK | MSG_WITNESS_FLAG;
   }
}
