namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Represents a block.
   /// </summary>
   public class Block
   {
      public BlockHeader? Header { get; set; }

      public Transaction[]? Transactions { get; set; }
   }
}
