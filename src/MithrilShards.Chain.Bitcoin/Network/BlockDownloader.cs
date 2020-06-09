using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network
{
   /// <summary>
   /// Manage the download blocking
   /// </summary>
   public class BlockDownloader : IBlockDownloader
   {
      private readonly object theLock = new object();

      readonly ILogger<BlockDownloader> logger;

      private readonly Dictionary<UInt256, IPeerContext> downloadingBlocks = new Dictionary<UInt256, IPeerContext>();

      public int BlocksInDownload => this.downloadingBlocks.Count;

      public BlockDownloader(ILogger<BlockDownloader> logger)
      {
         this.logger = logger;
      }

      public bool IsDownloading(HeaderNode header)
      {
         return this.downloadingBlocks.ContainsKey(header.Hash);
      }

      public bool TryDownloadBlock(PeerContext peerContext, HeaderNode headerNode, [MaybeNullWhen(false)] out QueuedBlock queuedBlock)
      {
         UInt256 blockHash = headerNode.Hash;
         queuedBlock = null;

         lock (this.theLock)
         {
            if (this.downloadingBlocks.TryGetValue(blockHash, out IPeerContext? peerDownloadingBlock) && peerDownloadingBlock == peerContext)
            {
               //TODO: bitcoin core returns block this peer is downloading, not sure it's the right approach
               return false;
            }

            this.MarkBlockAsReceived(blockHash);

            this.downloadingBlocks.Add(blockHash, peerContext);

            queuedBlock = new QueuedBlock(headerNode);
            return true;
         }
      }

      // Returns a bool indicating whether we requested this block.
      // Also used if a block was /not/ received and timed out or started with another peer
      private bool MarkBlockAsReceived(UInt256 hash)
      {
         //std::map<uint256, std::pair<NodeId, std::list<QueuedBlock>::iterator>>::iterator itInFlight = mapBlocksInFlight.find(hash);
         //if (itInFlight != mapBlocksInFlight.end())
         //{
         //   CNodeState* state = State(itInFlight->second.first);
         //   assert(state != nullptr);
         //   state->nBlocksInFlightValidHeaders -= itInFlight->second.second->fValidatedHeaders;
         //   if (state->nBlocksInFlightValidHeaders == 0 && itInFlight->second.second->fValidatedHeaders)
         //   {
         //      // Last validated block on the queue was received.
         //      nPeersWithValidatedDownloads--;
         //   }
         //   if (state->vBlocksInFlight.begin() == itInFlight->second.second)
         //   {
         //      // First block on the queue was received, update the start download time for the next one
         //      state->nDownloadingSince = std::max(state->nDownloadingSince, GetTimeMicros());
         //   }
         //   state->vBlocksInFlight.erase(itInFlight->second.second);
         //   state->nBlocksInFlight--;
         //   state->nStallingSince = 0;
         //   mapBlocksInFlight.erase(itInFlight);
         //   return true;
         //}
         //return false;

         //TODO
         return true;
      }
   }
}
