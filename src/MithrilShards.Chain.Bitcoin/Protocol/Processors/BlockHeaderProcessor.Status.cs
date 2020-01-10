namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      private readonly Status status = new Status();

      private class Status
      {
         public bool UseCompactBlocks { get; internal set; } = false;
      }
   }
}