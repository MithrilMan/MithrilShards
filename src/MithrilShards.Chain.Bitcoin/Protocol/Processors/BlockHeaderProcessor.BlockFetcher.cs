using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor : IBlockFetcher
   {
      public uint GetScore()
      {
         return 1; //TODO
      }

      public uint GetFetchBlockScore(HeaderNode blockToBeEvaluated)
      {
         bool isAvailable = this.status.BestKnownHeader?.IsInSameChain(blockToBeEvaluated) == false;
         bool canServe = (!this.IsWitnessEnabled(blockToBeEvaluated.Previous) || this.status.CanServeWitness);
         return (isAvailable && canServe) ? GetScore() + 100 : 0;
      }

      public bool TryFetch(HeaderNode blockToDownload, uint minimumScore)
      {
         var blockScore = GetFetchBlockScore(blockToDownload);
         if (blockScore < minimumScore || blockScore == 0)
         {
            this.logger.LogDebug("Cannot download the block, score {BlockScore} doesn't satisfy requirement {MinimumScore}", blockScore, minimumScore);
            return false;
         }

         FetchBlock(blockToDownload);
         return true;
      }

      private void FetchBlock(HeaderNode blockToDownload)
      {
         //if (!currentHeader.Validity.HasFlag(HeaderDataAvailability.HasBlockData)  // we don't have data for this block
         //   && !this.blockFetcherManager.IsDownloading(currentHeader) // it's not already in download
         //   && (!this.IsWitnessEnabled(currentHeader.Previous) || this.status.CanServeWitness) //witness isn't enabled or the other peer can't serve witness
         //   )
         //{
         //   blocksToDownload.Add(currentHeader);
         //}

         //currentHeader = currentHeader.Previous;

         //var vGetData = new List<InventoryVector>();
         //// Download as much as possible, from earliest to latest.
         //for (int i = blocksToDownload.Count - 1; i >= 0; i--)
         //{
         //   HeaderNode blockToDownload = blocksToDownload[i];
         //   UInt256 blockHash = blockToDownload.Hash;

         //   if (this.blockDownloadStatus.BlocksInDownload.Count >= MAX_BLOCKS_IN_TRANSIT_PER_PEER)
         //   {
         //      break; // Can't download any more from this peer
         //   }

         //   uint fetchFlags = this.GetFetchFlags();

         //   if (ShouldRequestCompactBlock(lastValidatedHeaderNode))
         //   {
         //      vGetData.Add(new InventoryVector { Type = InventoryType.MSG_CMPCT_BLOCK, Hash = blockHash });
         //   }
         //   else
         //   {
         //      vGetData.Add(new InventoryVector { Type = InventoryType.MSG_BLOCK | fetchFlags, Hash = blockHash });
         //   }

         //   if (this.blockFetcherManager.TryDownloadBlock(this.PeerContext, blockToDownload, out QueuedBlock? queuedBlock))
         //   {
         //      if (this.blockDownloadStatus.BlocksInDownload.Count == 1)
         //      {
         //         // We're starting a block download (batch) from this peer.
         //         this.blockDownloadStatus.DownloadingSince = this.dateTimeProvider.GetTime();
         //      }
         //   }

         //   this.logger.LogDebug("Requesting block {BlockHash}", blockHash);
         //}

         //if (vGetData.Count > 0)
         //{
         //   this.logger.LogDebug("Downloading blocks toward {HeaderNode} via headers direct fetch.", lastValidatedHeaderNode);

         //   await this.SendMessageAsync(new GetDataMessage { Inventory = vGetData.ToArray() }).ConfigureAwait(false);
         //}
      }






      /// <summary>
      /// Update LastCommonBlock and add not-in-flight missing successors to vBlocks, until it has at most count entries.
      /// </summary>
      /// <returns>Blocks to download</returns>
      public List<HeaderNode>? FindNextBlocksToDownload(out IBlockFetcher? nodeStaller)
      {
         List<HeaderNode> blocksToDownload = new List<HeaderNode>(MAX_BLOCKS_IN_TRANSIT_PER_PEER);

         nodeStaller = null;

         int count = fetcherStatus.BlocksInDownload.Count;
         if (count == 0) return null; //no blocks to download

         // Make sure pindexBestKnownBlock is up to date, we'll need it.
         ProcessBlockAvailability();

         if (status.BestKnownHeader == null
            || status.BestKnownHeader.ChainWork < this.chainState.GetTip().ChainWork
            || status.BestKnownHeader.ChainWork < this.minimumChainWork)
         {
            // This peer has nothing interesting.
            return null;
         }

         if (fetcherStatus.LastCommonBlock == null)
         {
            // Bootstrap quickly by guessing a parent of our best tip is the forking point.
            // Guessing wrong in either direction is not a problem.
            this.chainState.TryGetAtHeight(Math.Min(status.BestKnownHeader.Height, this.chainState.GetTip().Height), out HeaderNode? headerNode);
            fetcherStatus.LastCommonBlock = headerNode;
         }

         // If the peer reorganized, our previous pindexLastCommonBlock may not be an ancestor
         // of its current tip anymore. Go back enough to fix that.
         fetcherStatus.LastCommonBlock = fetcherStatus.LastCommonBlock!.LastCommonAncestor(status.BestKnownHeader);
         if (fetcherStatus.LastCommonBlock == status.BestKnownHeader)
         {
            return null;
         }

         List<HeaderNode> vToFetch = new List<HeaderNode>(128);
         HeaderNode pindexWalk = this.fetcherStatus.LastCommonBlock;
         // Never fetch further than the best block we know the peer has, or more than BLOCK_DOWNLOAD_WINDOW + 1 beyond the last
         // linked block we have in common with this peer. The +1 is so we can detect stalling, namely if we would be able to
         // download that next block if the window were 1 larger.
         int nWindowEnd = fetcherStatus.LastCommonBlock.Height + BLOCK_DOWNLOAD_WINDOW;
         int nMaxHeight = Math.Min(status.BestKnownHeader.Height, nWindowEnd + 1);
         IBlockFetcher? waitingfor = null;
         while (pindexWalk.Height < nMaxHeight)
         {
            // Read up to 128 (or more, if more blocks than that are needed) successors of pindexWalk (towards
            // pindexBestKnownBlock) into vToFetch. We fetch 128, because CBlockIndex::GetAncestor may be as expensive
            // as iterating over ~100 CBlockIndex* entries anyway.
            int toFetchSize = Math.Min(nMaxHeight - pindexWalk.Height, Math.Max(count - blocksToDownload.Count, 128));
            vToFetch.Clear();
            pindexWalk = status.BestKnownHeader.GetAncestor(pindexWalk.Height + toFetchSize)!;
            vToFetch[toFetchSize - 1] = pindexWalk;
            for (int i = toFetchSize - 1; i > 0; i--)
            {
               vToFetch[i - 1] = vToFetch[i].Previous!;
            }

            // Iterate over those blocks in vToFetch (in forward direction), adding the ones that
            // are not yet downloaded and not in flight to vBlocks. In the meantime, update
            // pindexLastCommonBlock as long as all ancestors are already downloaded, or if it's
            // already part of our chain (and therefore don't need it even if pruned).
            foreach (HeaderNode pindex in vToFetch)
            {
               if (!pindex.IsValid(HeaderValidityStatuses.ValidTree))
               {
                  // We consider the chain that this peer is on invalid.
                  return blocksToDownload;
               }
               if (!status.CanServeWitness && IsWitnessEnabled(pindex.Previous))
               {
                  // We wouldn't download this block or its descendants from this peer.
                  return blocksToDownload;
               }
               if (pindex.HasAvailability(HeaderDataAvailability.HasBlockData) || this.chainState.IsInBestChain(pindex))
               {
                  if (pindex.HaveTxsDownloaded())
                  {
                     fetcherStatus.LastCommonBlock = pindex;
                  }
               }
               else if (!this.blockFetcherManager.TryGetFetcher(pindex.Hash, out IBlockFetcher? fetcherDownloadingBlock))
               {
                  // The block is not already downloaded, and not yet in flight.
                  if (pindex.Height > nWindowEnd)
                  {
                     // We reached the end of the window.
                     if (blocksToDownload.Count == 0 && waitingfor != this)
                     {
                        // We aren't able to fetch anything, but we would be if the download window was one larger.
                        nodeStaller = waitingfor;
                     }
                     return blocksToDownload;
                  }
                  blocksToDownload.Add(pindex);
                  if (blocksToDownload.Count == count)
                  {
                     return blocksToDownload;
                  }
               }
               else if (waitingfor == null)
               {
                  // This is the first already-in-flight block.
                  waitingfor = fetcherDownloadingBlock;
               }
            }
         }

         return blocksToDownload;
      }





      BlockFetcherStatistics fetcherStatus = new BlockFetcherStatistics();

      public class BlockFetcherStatistics
      {
         public HeaderNode? LastCommonBlock { get; set; }
         /// <summary>
         /// Gets the date when the peer started to download blocks.
         /// </summary>
         public long DownloadingSince { get; set; } = 0;

         /// <summary>
         /// The list of blocks this peer is asked to download.
         /// </summary>
         public List<QueuedBlock> BlocksInDownload { get; } = new List<QueuedBlock>();

         /// <summary>
         /// Since when we're stalling block download progress (in microseconds), or 0.
         /// </summary>
         public long StallingSince { get; set; }
      }
   }
}