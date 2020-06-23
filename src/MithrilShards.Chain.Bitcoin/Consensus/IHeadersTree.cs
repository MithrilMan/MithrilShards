using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IHeadersTree
   {
      HeaderNode Genesis { get; }

      int Height { get; }

      void Add(in HeaderNode newHeader);

      BlockLocator GetTipLocator();

      BlockLocator? GetLocator(UInt256 blockHash);

      /// <summary>
      /// Gets the current tip header node.
      /// </summary>
      /// <returns></returns>
      HeaderNode GetTip();

      bool IsInBestChain(HeaderNode? headerNode);

      /// <summary>
      /// Determines whether the specified hash is a known hash.
      /// May be present on best chain or on a fork.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <returns>
      ///   <c>true</c> if the specified hash is known; otherwise, <c>false</c>.
      /// </returns>
      bool IsKnown(UInt256? hash);

      bool TryGetHash(int height, [MaybeNullWhen(false)] out UInt256 blockHash);

      bool TryGetNode(UInt256? blockHash, bool onlyBestChain, [MaybeNullWhen(false)] out HeaderNode node);

      bool TryGetNodeOnBestChain(int height, [MaybeNullWhen(false)] out HeaderNode node);
   }
}