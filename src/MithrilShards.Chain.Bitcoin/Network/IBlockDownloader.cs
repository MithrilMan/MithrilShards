using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public interface IBlockDownloader
   {
      int BlocksInDownload { get; }

      /// <summary>
      /// Tries to download the block from the specified <paramref name="peerContext"/>.
      /// </summary>
      /// <param name="peerContext">The peer context from which to download the block.</param>
      /// <param name="blockToDownload">The block to download.</param>
      /// <param name="queuedBlock">The queued block.</param>
      /// <returns></returns>
      bool TryDownloadBlock(PeerContext peerContext, HeaderNode blockToDownload, [MaybeNullWhen(false)] out QueuedBlock queuedBlock);

      /// <summary>
      /// Determines whether the specified header is downloading.
      /// </summary>
      /// <param name="header">The header.</param>
      /// <returns>
      ///   <c>true</c> if the specified header is downloading; otherwise, <c>false</c>.
      /// </returns>
      bool IsDownloading(HeaderNode header);
   }
}