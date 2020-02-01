namespace MithrilShards.Core
{
   /// <summary>
   /// Defines method to check if current node is in initial block download state.
   /// </summary>
   public interface IInitialBlockDownloadTracker
   {
      public bool IsDownloadingBlocks();
   }
}
