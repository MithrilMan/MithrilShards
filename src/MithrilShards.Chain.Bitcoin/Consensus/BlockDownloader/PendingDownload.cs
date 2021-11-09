namespace MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;

/// <summary>
/// Tracks the ongoing block fetch operation.
/// </summary>
public class PendingDownload
{
   /// <summary>
   /// Gets the HeaderNode representing the block to fetch.
   /// </summary>
   public HeaderNode BlockInDownload { get; }

   /// <summary>
   /// Gets the block fetcher chosen to fetch the block.
   /// </summary>
   public IBlockFetcher BlockFetcher { get; }

   /// <summary>
   /// Gets the block fetch starting time (usec).
   /// </summary>
   public long StartingTime { get; }

   public PendingDownload(HeaderNode blockInDownload, IBlockFetcher blockFetcher, long startingTime)
   {
      BlockInDownload = blockInDownload;
      BlockFetcher = blockFetcher;
      StartingTime = startingTime;
   }
}
