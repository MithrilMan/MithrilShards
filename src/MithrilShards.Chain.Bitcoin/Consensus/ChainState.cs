using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Stores and provides an API to update our local knowledge of the current best chain.
   /// Anything that is related on current chain tip is stored here, whereas block information
   /// and metadata independent of the current tip is kept in other classes like <see cref="Consensus.HeadersTree"/> and similar.
   /// </summary>
   public class ChainState : IChainState
   {
      protected readonly ILogger<ChainState> logger;

      readonly IBlockHeaderRepository blockHeaderRepository;

      /// <summary>
      /// The current chain of block headers we consult and build on.
      /// </summary>
      public IHeadersTree HeadersTree { get; }

      /// <summary>
      /// Allows to have a view on coins and UTXO set.
      /// </summary>
      /// <value>
      /// The coins view.
      /// </value>
      protected private readonly ICoinsView coinsView;

      readonly IInitialBlockDownloadTracker initialBlockDownloadTracker;

      /// <summary>
      /// Gets or sets the block sequence identifier.
      /// </summary>
      /// <remarks>Blocks loaded from disk are assigned id 0, so start the counter at 1.</remarks>
      /// <value>
      /// The block sequence identifier.
      /// </value>
      protected int blockSequenceId = 1;

      /// <summary>
      /// Gets or sets the block reverse sequence identifier.
      /// Decreasing counter (used by subsequent preciousblock calls).
      /// </summary>
      /// <value>
      /// The block reverse sequence identifier.
      /// </value>
      protected int blockReverseSequenceId = -1;

      /// <summary>
      /// The last precious chainwork.
      /// Chainwork for the last block that preciousblock has been applied to.
      /// </summary>
      protected Target lastPreciousChainwork = Target.Zero;

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

      public ChainState(ILogger<ChainState> logger,
                        IHeadersTree headersTree,
                        ICoinsView coinsView,
                        IBlockHeaderRepository blockHeaderRepository,
                        IInitialBlockDownloadTracker initialBlockDownloadTracker)
      {
         this.logger = logger;
         this.HeadersTree = headersTree;
         this.coinsView = coinsView;
         this.blockHeaderRepository = blockHeaderRepository;
         this.initialBlockDownloadTracker = initialBlockDownloadTracker;

         this.BestChainTip = headersTree.Genesis;
         this.ValidatedHeadersTip = headersTree.Genesis;
      }

      /// <summary>
      /// Signals to related components that current chain state has to be committed (flushed on disk for example)
      /// </summary>
      public void Commit()
      {

      }

      public BlockLocator GetTipLocator()
      {
         using var readMainLock = GlobalLocks.ReadOnMain();
         return this.HeadersTree.GetTipLocator();
      }

      public bool TryGetBestChainHeaderNode(UInt256 blockHash, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using var readMainLock = GlobalLocks.ReadOnMain();
         return this.HeadersTree.TryGetNode(blockHash, true, out node);
      }

      public bool TryGetKnownHeaderNode(UInt256? blockHash, [MaybeNullWhen(false)] out HeaderNode node)
      {
         //using var readLock = new ReadLock(this.theLock);
         using var readMainLock = GlobalLocks.ReadOnMain();
         return this.HeadersTree.TryGetNode(blockHash, false, out node);
      }


      public BlockLocator? GetLocator(UInt256 blockHash)
      {
         using var readMainLock = GlobalLocks.ReadOnMain();
         return this.HeadersTree.GetLocator(blockHash);
      }

      public bool IsInBestChain(HeaderNode headerNode)
      {
         return this.HeadersTree.IsInBestChain(headerNode);
      }

      public HeaderNode GetTip()
      {
         return this.BestChainTip;
      }

      public BlockHeader GetTipHeader()
      {
         using var readMainLock = GlobalLocks.ReadOnMain();
         if (!this.blockHeaderRepository.TryGet(this.BestChainTip.Hash, out BlockHeader? header))
         {
            ThrowHelper.ThrowBlockHeaderRepositoryException($"Unexpected error, cannot fetch the tip at height {this.BestChainTip.Height}.");
         }

         return header!;
      }

      public HeaderNode FindForkInGlobalIndex(BlockLocator locator)
      {
         using (GlobalLocks.ReadOnMain())
         {

            // Find the latest block common to locator and chain - we expect that
            // locator.vHave is sorted descending by height.
            foreach (UInt256? hash in locator.BlockLocatorHashes)
            {
               if (TryGetKnownHeaderNode(hash, out HeaderNode? pindex))
               {
                  if (IsInBestChain(pindex))
                  {
                     return pindex;
                  }

                  if (pindex.GetAncestor(this.BestChainTip.Height) == this.BestChainTip)
                  {
                     return this.BestChainTip;
                  }
               }
            }
         }
         return this.HeadersTree.Genesis;
      }


      public bool TryGetNext(HeaderNode headerNode, [MaybeNullWhen(false)] out HeaderNode nextHeaderNode)
      {
         using (GlobalLocks.ReadOnMain())
         {
            if (
               this.HeadersTree.TryGetNodeOnBestChain(headerNode.Height + 1, out nextHeaderNode)
               && nextHeaderNode.Previous?.Hash == nextHeaderNode.Hash
               )
            {
               return true;
            }

            nextHeaderNode = null;
            return false;
         }
      }

      public bool TryGetBlockHeader(HeaderNode headerNode, [MaybeNullWhen(false)] out BlockHeader blockHeader)
      {
         return this.blockHeaderRepository.TryGet(headerNode.Hash, out blockHeader);
      }

      ///TODO valutare se lasciare qui solo cose inerenti la TIP, lasciare il resto in HeadersTree in modo che si possano fare
      ///query dirette sui known headers e best chain
   }
}