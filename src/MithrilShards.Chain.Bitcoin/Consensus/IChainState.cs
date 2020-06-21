using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IChainState
   {
      /// <summary>
      /// Gets the tip of the best chain.
      /// </summary>
      /// <value>
      /// The best chain tip.
      /// </value>
      public HeaderNode BestChainTip { get; }

      /// <summary>
      /// Gets the tip of the best validated header.
      /// It may not be the header of current best chain.
      /// </summary>
      /// <value>
      /// The tip of the best validated header.
      /// </value>
      public HeaderNode ValidatedHeadersTip { get; }

      BlockLocator GetTipLocator();

      void Commit();

      /// <summary>
      /// Tries to get the a <see cref="HeaderNode"/> from an hash, looking in the best chain.
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="height">The height if found, -1 otherwise.</param>
      /// <returns><c>true</c> if the result has been found, <see langword="false"/> otherwise.</returns>
      bool TryGetBestChainHeaderNode(UInt256 blockHash, [MaybeNullWhen(false)] out HeaderNode node);

      /// <summary>
      /// Returns the first common block between our known best chain and the block locator.
      /// </summary>
      /// <param name="hashes">Hash to search for</param>
      /// <returns>First found block or genesis</returns>
      HeaderNode GetHighestNodeInBestChainFromBlockLocator(BlockLocator blockLocator);

      /// <summary>
      /// Determines whether the specified hash is a known hash.
      /// May be present on best chain or on a fork.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <returns>
      ///   <c>true</c> if the specified hash is known; otherwise, <c>false</c>.
      /// </returns>
      bool TryGetKnownHeaderNode(UInt256? hash, [MaybeNullWhen(false)] out HeaderNode node);

      bool IsInBestChain(HeaderNode hash);

      /// <summary>
      /// Gets the best chain tip header node.
      /// </summary>
      /// <returns></returns>
      HeaderNode GetTip();

      /// <summary>
      /// Gets the best chain tip block header.
      /// </summary>
      /// <returns></returns>
      BlockHeader GetTipHeader();

      BlockLocator? GetLocator(UInt256 blockHash);
   }
}