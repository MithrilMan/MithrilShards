using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

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
         return CanFetch(blockToBeEvaluated) ? GetScore() + 100 : 0;
      }

      private bool CanFetch(HeaderNode node)
      {
         bool isAvailable = _status.BestKnownHeader?.IsInSameChain(node) == true;
         bool canServe = (!IsWitnessEnabled(node.Previous) || PeerContext.CanServeWitness);
         return isAvailable && canServe;
      }

      public async Task<bool> TryFetchAsync(HeaderNode blockToDownload, uint minimumScore)
      {
         uint blockScore = GetFetchBlockScore(blockToDownload);
         if (blockScore < minimumScore || blockScore == 0)
         {
            logger.LogDebug("Cannot download the block, score {BlockScore} doesn't satisfy requirement {MinimumScore}", blockScore, minimumScore);
            return false;
         }

         await FetchBlockAsync(blockToDownload).ConfigureAwait(false);
         return true;
      }


      private async Task FetchBlockAsync(HeaderNode blockToDownload)
      {
         #region Code that was into the headers processor to request block in place
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
         #endregion

         /// when this method is called, we already gave back our availability to download this header so we shouldn't
         /// have to repeat checks we are already enforcing elsewhere, but we do it anyway to be safe (consider if remove checks)

         // we don't have data for this block and we can fetch it
         if (!blockToDownload.HasAvailability(HeaderDataAvailability.HasBlockData) && CanFetch(blockToDownload))
         {
            _fetcherStatus.BlocksInDownload.Add(blockToDownload.Hash);

            var vGetData = new List<InventoryVector>();
            uint fetchFlags = GetFetchFlags();
            vGetData.Add(new InventoryVector { Type = InventoryType.MSG_BLOCK | fetchFlags, Hash = blockToDownload.Hash });

            await SendMessageAsync(new GetDataMessage { Inventory = vGetData.ToArray() }).ConfigureAwait(false);
         }
      }

      readonly BlockFetcherStatistics _fetcherStatus = new BlockFetcherStatistics();

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
         public List<UInt256> BlocksInDownload { get; } = new List<UInt256>();

         /// <summary>
         /// Since when we're stalling block download progress (in microseconds), or 0.
         /// </summary>
         public long StallingSince { get; set; }
      }
   }
}