using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IHeadersTree
   {
      HeaderNode Genesis { get; }

      void Add(in HeaderNode newHeader);

      /// <summary>
      /// Gets the block locator starting from passed <paramref name="headerNode"/>.
      /// </summary>
      /// <param name="headerNode">The header node representing last known header of the block locator.</param>
      /// <returns></returns>
      BlockLocator? GetLocator(HeaderNode headerNode);

      /// <summary>
      /// Gets the current tip header node.
      /// </summary>
      /// <returns></returns>
      HeaderNode GetTip();

      bool IsInBestChain(HeaderNode? headerNode);

      bool TryGetNode(UInt256? blockHash, bool onlyBestChain, [MaybeNullWhen(false)] out HeaderNode node);

      bool TryGetNodeOnBestChain(int height, [MaybeNullWhen(false)] out HeaderNode node);
   }
}