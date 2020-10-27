using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader
{
   public interface IBlockFetcherManager : IHostedService
   {
      /// <summary>
      /// Determines whether the specified header is downloading.
      /// </summary>
      /// <param name="header">The header.</param>
      /// <returns>
      ///   <c>true</c> if the specified header is downloading; otherwise, <c>false</c>.
      /// </returns>
     // bool IsDownloading(UInt256 header);


      /// <summary>
      /// Registers the passed block fetcher.
      /// A registered fetcher can be asked to download a block at any time
      /// </summary>
      /// <param name="blockFetcher">The block fetcher.</param>
      void RegisterFetcher(IBlockFetcher blockFetcher);

      /// <summary>
      /// Requires the assignment to download the specified block.
      /// A block may be required to be downloaded from a specific fetcher for different reasons,
      /// one of which may be the fact that the fetcher is a peer who need this block to continue to validate the chain.
      /// Requiring an explicit assignment tries to bypass the automatic assignment behavior of the <see cref="IBlockFetcherManager"/> implementation.
      /// </summary>
      /// <param name="fetcher">The fetcher requiring the assignment.</param>
      /// <param name="requestedBlock">The requested block.</param>
      void RequireAssignment(IBlockFetcher fetcher, HeaderNode requestedBlock);

      bool TryGetFetcher(UInt256 hash, [MaybeNullWhen(false)] out IBlockFetcher? fetcher);
   }
}