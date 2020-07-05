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


      bool TryGetFetcher(UInt256 hash, [MaybeNullWhen(false)] out IBlockFetcher? fetcher);
   }
}