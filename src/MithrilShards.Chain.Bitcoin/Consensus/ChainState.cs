using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus;

/// <summary>
/// Stores and provides an API to update our local knowledge of the current best chain.
/// Anything that is related on current chain tip is stored here, whereas block information
/// and metadata independent of the current tip is kept in other classes like <see cref="Consensus.HeadersTree"/> and similar.
/// </summary>
public class ChainState : IChainState
{
   protected readonly ILogger<ChainState> logger;

   readonly IBlockHeaderRepository _blockHeaderRepository;

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
   private protected readonly ICoinsView coinsView;

   readonly IConsensusParameters _consensusParameters;

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
   public HeaderNode ChainTip { get; private set; }

   /// <summary>
   /// Gets the best validated header we know so far.
   /// It may not be the header of current best chain during IBD or during initial phases of a reorg.
   /// </summary>
   /// <value>
   /// The best validated header we know so far.
   /// </value>
   public HeaderNode BestHeader { get; private set; }

   public ChainState(ILogger<ChainState> logger,
                     IHeadersTree headersTree,
                     ICoinsView coinsView,
                     IBlockHeaderRepository blockHeaderRepository,
                     IConsensusParameters consensusParameters)
   {
      this.logger = logger;
      HeadersTree = headersTree;
      this.coinsView = coinsView;
      _blockHeaderRepository = blockHeaderRepository;
      _consensusParameters = consensusParameters;
      ChainTip = headersTree.Genesis;
      BestHeader = headersTree.Genesis;

      _blockHeaderRepository.TryAddAsync(consensusParameters.GenesisHeader);
   }

   /// <summary>
   /// Signals to related components that current chain state has to be committed (flushed on disk for example)
   /// </summary>
   public void Commit()
   {

   }

   public BlockLocator GetTipLocator()
   {
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readMainLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();
      return HeadersTree.GetLocator(ChainTip)!;
   }

   public bool TryGetBestChainHeaderNode(UInt256 blockHash, [MaybeNullWhen(false)] out HeaderNode node)
   {
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readMainLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();
      return HeadersTree.TryGetNode(blockHash, true, out node);
   }

   public bool TryGetKnownHeaderNode(UInt256? blockHash, [MaybeNullWhen(false)] out HeaderNode node)
   {
      //using var readLock = new ReadLock(this.theLock);
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readMainLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();
      return HeadersTree.TryGetNode(blockHash, false, out node);
   }


   public BlockLocator? GetLocator(HeaderNode headerNode)
   {
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readMainLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();
      return HeadersTree.GetLocator(headerNode);
   }

   public bool IsInBestChain(HeaderNode headerNode)
   {
      return HeadersTree.IsInBestChain(headerNode);
   }

   public HeaderNode GetTip()
   {
      return ChainTip;
   }

   public BlockHeader GetTipHeader()
   {
      using Microsoft.VisualStudio.Threading.AsyncReaderWriterLock.Releaser readMainLock = GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult();
      if (!_blockHeaderRepository.TryGet(ChainTip.Hash, out BlockHeader? header))
      {
         ThrowHelper.ThrowBlockHeaderRepositoryException($"Unexpected error, cannot fetch the tip at height {ChainTip.Height}.");
      }

      return header!;
   }

   public HeaderNode FindForkInGlobalIndex(BlockLocator locator)
   {
      using (GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult())
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

               if (pindex.GetAncestor(ChainTip.Height) == ChainTip)
               {
                  return ChainTip;
               }
            }
         }
      }
      return HeadersTree.Genesis;
   }


   public bool TryGetNext(HeaderNode headerNode, [MaybeNullWhen(false)] out HeaderNode nextHeaderNode)
   {
      using (GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult())
      {
         if (IsInBestChain(headerNode) && HeadersTree.TryGetNodeOnBestChain(headerNode.Height + 1, out nextHeaderNode))
         {
            return true;
         }

         nextHeaderNode = null;
         return false;
      }
   }

   public bool TryGetBlockHeader(HeaderNode headerNode, [MaybeNullWhen(false)] out BlockHeader blockHeader)
   {
      return _blockHeaderRepository.TryGet(headerNode.Hash, out blockHeader);
   }

   public bool TryGetAtHeight(int height, [MaybeNullWhen(false)] out HeaderNode? headerNode)
   {
      using (GlobalLocks.ReadOnMainAsync().GetAwaiter().GetResult())
      {
         return HeadersTree.TryGetNodeOnBestChain(height, out headerNode);
      }
   }

   public HeaderNode AddToBlockIndex(BlockHeader header)
   {
      using (GlobalLocks.WriteOnMainAsync().GetAwaiter().GetResult())
      {

         // Check for duplicate
         if (TryGetKnownHeaderNode(header.Hash, out HeaderNode? headerNode))
         {
            return headerNode;
         }

         if (!TryGetKnownHeaderNode(header.PreviousBlockHash, out HeaderNode? previousHeader))
         {
            ThrowHelper.ThrowNotSupportedException("Previous hash not found (shouldn't happen).");
         }

         headerNode = new HeaderNode(header, previousHeader);

         if (BestHeader == null || BestHeader.ChainWork < headerNode.ChainWork)
         {
            BestHeader = headerNode;
         }

         HeadersTree.Add(headerNode);
         _blockHeaderRepository.TryAddAsync(header);

         return headerNode;
      }
   }
}
