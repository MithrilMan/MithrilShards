namespace MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader
{
   /// <summary>
   /// Represents a block queued to be downloaded.
   /// </summary>
   /// <seealso cref="Network.IBlockDownloader" />
   public class QueuedBlock
   {
      /// <summary>
      /// Gets the header node.
      /// </summary>
      /// <value>
      /// The header node.
      /// </value>
      public HeaderNode HeaderNode { get; }

      /// <summary>
      /// Gets the fetcher that chosen to download the block.
      /// </summary>
      /// <value>
      /// The fetcher.
      /// </value>
      public IBlockFetcher Fetcher { get; }

      public QueuedBlock(HeaderNode headerNode, IBlockFetcher fetcher /*TODO: ,PartiallyDownloadedBlock partialBlock*/)
      {
         HeaderNode = headerNode;
         Fetcher = fetcher;
      }
   }
}