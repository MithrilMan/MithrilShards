using System.Threading.Tasks;

namespace MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;

/// <summary>
/// Interface that anyone able to fetch blocks should implement.
/// A fetcher, in order to be eligible to fetch a block, has to register
/// to an <see cref="IBlockFetcherManager"/> implementation using
/// <see cref="IBlockFetcherManager.RegisterFetcher(IBlockFetcher)"/>
/// </summary>
public interface IBlockFetcher
{
   /// <summary>
   /// Gets the score of this fetcher.
   /// This score takes into account of the peer speed in general, the service available by the fetcher, the trust level, etc..
   /// (e.g. if the fetcher is a peer, a better score could be given to peers supporting segwit or specific services, or if
   /// the peer is a whitelisted peer or if its behavior score is high, etc...
   /// </summary>
   /// <returns></returns>
   uint GetScore();

   /// <summary>
   /// Gets the fetch score for this specific block hash request.
   /// A return value of 0 means this peer can't fetch this block.
   /// </summary>
   /// <param name="blockToBeEvaluated">The block to download for which we want to evaluate the score.</param>
   /// <returns></returns>
   uint GetFetchBlockScore(HeaderNode blockToBeEvaluated);

   /// <summary>
   /// Tries the fetch the specified hash if minimum score is met.
   /// </summary>
   /// <param name="blockToDownload">The block to download.</param>
   /// <param name="minimumScore">The minimum score needed to fetch the block.</param>
   /// <returns></returns>
   Task<bool> TryFetchAsync(HeaderNode blockToDownload, uint minimumScore);

   ///// <summary>
   ///// Signals to the fetcher to stop the download of the specified block, if possible.
   ///// </summary>
   ///// <param name="blockHash">The block hash.</param>
   //bool MarkAsReceived(UInt256 blockHash);
}
