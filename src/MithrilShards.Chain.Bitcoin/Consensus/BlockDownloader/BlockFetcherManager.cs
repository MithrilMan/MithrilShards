using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Events;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader
{
   /// <summary>
   /// Manage the download blocking
   /// </summary>
   public class BlockFetcherManager : IBlockFetcherManager, IPeriodicWorkExceptionHandler, IHostedService
   {
      /// <summary>
      /// The <see cref="checkStaleBlockFetchersLoop"/> interval, in seconds.
      /// </summary>
      private const int CHECK_STALE_BLOCK_FETCHERS_LOOP_INTERVAL = 60;

      /// <summary>
      /// The <see cref="checkFetchersScoreLoop"/> interval, in seconds.
      /// </summary>
      private const int CHECK_FETCHERS_SCORE_LOOP_INTERVAL = 60;

      /// <summary>
      /// The maximum number of blocks that can be fetched in parallel.
      /// </summary>
      private const int MAX_PARALLEL_DOWNLOADED_BLOCKS = 200;

      private static object channelLock = new object();
      private ReaderWriterLockSlim fetcherSlimLock = new ReaderWriterLockSlim();

      readonly ILogger<BlockFetcherManager> logger;
      readonly IEventBus eventBus;
      readonly IDateTimeProvider dateTimeProvider;
      readonly IChainState chainState;
      readonly IPeriodicWork blockFetchAssignmentLoop;
      readonly IPeriodicWork checkStaleBlockFetchersLoop;
      readonly IPeriodicWork checkFetchersScoreLoop;

      /// <summary>
      /// The list of registered fetchers
      /// </summary>
      private readonly List<IBlockFetcher> fetchers = new List<IBlockFetcher>();

      /// <summary>
      /// Contains the hashes of blocks we need to download because we have already their validated headers.
      /// </summary>
      private readonly ConcurrentQueue<HeaderNode> blocksToDownload = new ConcurrentQueue<HeaderNode>();

      /// <summary>
      /// Contains the hashes of blocks we failed to fetch.
      /// This list is more important than <see cref="blocksToDownload"/> because may refers to blocks
      /// closest to the tip
      /// </summary>
      private readonly ConcurrentDictionary<UInt256, HeaderNode> failedBlockFetch = new ConcurrentDictionary<UInt256, HeaderNode>();

      /// <summary>
      /// The blocks we are actually downloading
      /// </summary>
      private readonly ConcurrentDictionary<IBlockFetcher, List<PendingDownload>> blocksInDownload = new ConcurrentDictionary<IBlockFetcher, List<PendingDownload>>();

      /// <summary>
      /// Channel used to get data to send to fetchers.
      /// </summary>
      private readonly Channel<HeaderNode> requiredBlocks;

      Dictionary<UInt256, (IBlockFetcher fetcher, List<QueuedBlock> queuedBlocks)> mapBlocksInFlight = new Dictionary<UInt256, (IBlockFetcher, List<QueuedBlock>)>();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager eventSubscriptionManager = new EventSubscriptionManager();

      /// <summary>
      /// The fetchers score values computed regularly by <see cref="checkFetchersScoreLoop"/>
      /// </summary>
      private (uint min, uint max, uint average) fetchersScore;

      public BlockFetcherManager(ILogger<BlockFetcherManager> logger,
                                 IEventBus eventBus,
                                 IDateTimeProvider dateTimeProvider,
                                 IChainState chainState,
                                 IPeriodicWork downloadAssignmentLoop,
                                 IPeriodicWork checkStaleBlockDownload,
                                 IPeriodicWork checkFetcherScore)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.dateTimeProvider = dateTimeProvider;
         this.chainState = chainState;
         this.blockFetchAssignmentLoop = downloadAssignmentLoop;
         this.checkStaleBlockFetchersLoop = checkStaleBlockDownload;
         this.checkFetchersScoreLoop = checkFetcherScore;
         this.requiredBlocks = Channel.CreateUnbounded<HeaderNode>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

         this.blockFetchAssignmentLoop.Configure(stopOnException: false, exceptionHandler: this);
         this.checkStaleBlockFetchersLoop.Configure(stopOnException: false, exceptionHandler: this);
         this.checkFetchersScoreLoop.Configure(stopOnException: false, exceptionHandler: this);
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         // starts the loop that distribute block downloads requests.
         this.blockFetchAssignmentLoop.StartAsync(
            label: nameof(blockFetchAssignmentLoop),
            work: BlockFetchAssignmentWork,
            interval: TimeSpan.Zero,
            cancellationToken
            );

         // starts the loop that check for stale block fetchers.
         this.checkStaleBlockFetchersLoop.StartAsync(
            label: nameof(checkStaleBlockFetchersLoop),
            work: CheckStaleBlockFetchersWork,
            interval: TimeSpan.FromSeconds(CHECK_STALE_BLOCK_FETCHERS_LOOP_INTERVAL),
            cancellationToken
            );

         // starts the consumer loop that distribute block downloads requests.
         this.checkFetchersScoreLoop.StartAsync(
            label: nameof(checkFetchersScoreLoop),
            work: CheckFetchersScoreWork,
            interval: TimeSpan.FromSeconds(CHECK_FETCHERS_SCORE_LOOP_INTERVAL),
            cancellationToken
            );

         // subscribe to events we are interested into
         eventSubscriptionManager
            .RegisterSubscriptions(this.eventBus.Subscribe<BlockHeaderValidationSucceeded>(async (args) => await this.OnBlockHeaderValidationSucceeded(args).ConfigureAwait(false)))
            .RegisterSubscriptions(this.eventBus.Subscribe<BlockReceived>(this.OnBlockReceived));

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public void OnException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         this.logger.LogCritical("An unhandled exception has been raised in the {0} work.", failedWork.Label);
         feedback.ContinueExecution = false;
         feedback.IsCritical = true;
         feedback.Message = "Node may be unstable, restart the node to fix the problem";
      }

      public bool TryGetFetcher(UInt256 hash, [MaybeNullWhen(false)] out IBlockFetcher? fetcher)
      {
         bool result = this.mapBlocksInFlight.TryGetValue(hash, out (IBlockFetcher fetcher, List<QueuedBlock> queuedBlocks) items);
         fetcher = items.fetcher;

         return result;
      }

      private void OnBlockReceived(BlockReceived arg)
      {
         UInt256 blockHash = arg.ReceivedBlock.Header!.Hash!;

         //if we received a block from a fetcher, try to remove it from current downloading blocks
         if (arg.Fetcher != null)
         {
            //check if the block was requested and remove it from the queued blocks
            if (!this.blocksInDownload.TryGetValue(arg.Fetcher, out List<PendingDownload>? pendingDownloads))
            {
               this.logger.LogDebug("Received block {UnrequestedBlock} from an unexpected source, do nothing.", blockHash);
               return;
            }

            var currentPending = pendingDownloads.FirstOrDefault(d => d.BlockInDownload.Hash == blockHash);

            if (currentPending != null)
            {
               pendingDownloads.Remove(currentPending);

               // TODO: check the time it took to download and update the score?
               long elapsedUsec = dateTimeProvider.GetTimeMicros() - currentPending.StartingTime;
            }
         }
      }

      /// <summary>
      /// When headers gets validated and they were unknown headers, we want to download the blocks
      /// </summary>
      /// <remarks>
      /// During header sync, when we receive this event, a header validation occurred and from last validated header back
      /// to <see cref="BlockHeaderValidationSucceeded.NewHeadersFoundCount"/> previous header, we need to fetch these blocks.
      /// When we receive a block with an unknown header, we'll get this event too so to be sure to not request a block we
      /// have already, we ensure that the header node validity doesn't have the flag <see cref="HeaderDataAvailability.HasBlockData"/> set.
      /// </remarks>
      /// <param name="obj"></param>
      private async Task OnBlockHeaderValidationSucceeded(BlockHeaderValidationSucceeded args)
      {
         HeaderNode currentNode = args.LastValidatedHeaderNode;

         if (args.NewHeadersFoundCount > 0)
         {
            //check if the header is relative to current "best header"
            //this.chainState.
            var bestNodeHeader = this.chainState.BestHeader;
            if (args.LastValidatedHeaderNode.LastCommonAncestor(bestNodeHeader) != bestNodeHeader)
            {
               /// these validated headers refers to a chain that's not currently the best chain so I don't require blocks yet.
               /// Blocks are requested when the new validated headers cause the bestNodeHeader to switch on a fork

               ///TODO: ensure we are not stuck in case our best known header refers to something not validated fully
               ///(e.g. headers validate but blocks are never sent to us)
               ///Technically this branch may be left without blocks and so should be removed and rolled back to be moved then to a better fork
               this.logger.LogDebug("Ignoring validated headers because they are on a lower fork at this time");
               return;
            }

            List<HeaderNode> newNodesToFetch = new List<HeaderNode>();

            for (int i = 0; i < args.NewHeadersFoundCount; i++)
            {
               // ensure we don't have yet the data
               if (!currentNode.HasAvailability(HeaderDataAvailability.HasBlockData))
               {
                  newNodesToFetch!.Add(currentNode);
               }
               currentNode = currentNode.Previous!;
            }
            newNodesToFetch.Reverse();

            foreach (var header in newNodesToFetch)
            {
               this.blocksToDownload.Enqueue(header);
            }
         }

         await Task.Delay(50).ConfigureAwait(false); // a delay introduced to let the peer adjust it's block availability

         DownloadBlocksIfPossible();
      }

      public void RegisterFetcher(IBlockFetcher blockFetcher)
      {
         using (new WriteLock(fetcherSlimLock))
         {
            this.fetchers.Add(blockFetcher);
         }
      }

      /// <summary>
      /// try to enqueue blocks in case there are blocks to download and there are free download slots available.
      /// </summary>
      /// <returns></returns>
      private void DownloadBlocksIfPossible()
      {
         lock (channelLock)
         {
            if (this.blocksToDownload.Count == 0)
            {
               this.logger.LogDebug("No blocks to download");
               // no more blocks to download
               return;
            }

            int availableSlots = MAX_PARALLEL_DOWNLOADED_BLOCKS - this.blocksInDownload.Count;

            for (int i = 0; i < availableSlots; i++)
            {
               if (!this.blocksToDownload.TryDequeue(out HeaderNode? blockToDownload))
               {
                  this.logger.LogTrace("No blocks to download");
                  // no more blocks to download
                  return;
               }

               this.requiredBlocks.Writer.TryWrite(blockToDownload);
            }
         }
      }

      /// <summary>
      /// The consumer that perform validation.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      private async Task BlockFetchAssignmentWork(CancellationToken cancellation)
      {
         await foreach (HeaderNode blockToDownload in this.requiredBlocks.Reader.ReadAllAsync(cancellation))
         {
            // get first 3 fetchers with score
            //this.fetchers
            //   .Where(f => f.GetFetchBlockScore(blockToDownload.Hash) > this.fetchersScore.average)
            //   .Take(3)
            //   .DefaultIfEmpty(this.fetchers.)

            //try to see if performance wise has too much impact
            var selectedFetcher = (
               from fetcher in this.fetchers
               let score = fetcher.GetFetchBlockScore(blockToDownload)
               orderby score descending
               where score > 0 // only peers that can fetch the block
               select fetcher
               )
               .FirstOrDefault((fetcher) => fetcher.TryFetchAsync(blockToDownload, 0).ConfigureAwait(false).GetAwaiter().GetResult()); //TODO use async

            if (selectedFetcher == null)
            {
               this.logger.LogDebug("None of the block fetcher is able to get block {BlockHash}, mark this block as failed", blockToDownload.Hash);
               this.failedBlockFetch.TryAdd(blockToDownload.Hash, blockToDownload);
               return;
            }

            if (!this.blocksInDownload.TryGetValue(selectedFetcher, out List<PendingDownload>? selectedFetcherPendingDownloads))
            {
               selectedFetcherPendingDownloads = new List<PendingDownload>();
               this.blocksInDownload.TryAdd(selectedFetcher, selectedFetcherPendingDownloads);
            }

            selectedFetcherPendingDownloads.Add(new PendingDownload(blockToDownload, selectedFetcher, this.dateTimeProvider.GetTimeMicros()));
         }
      }

      private Task CheckStaleBlockFetchersWork(CancellationToken cancellation)
      {
         //TODO L4240
         //if (state.nStallingSince && state.nStallingSince < nNow - 1000000 * BLOCK_STALLING_TIMEOUT)
         //{
         //   // Stalling only triggers when the block download window cannot move. During normal steady state,
         //   // the download window should be much larger than the to-be-downloaded set of blocks, so disconnection
         //   // should only happen during initial block download.
         //   LogPrintf("Peer=%d is stalling block download, disconnecting\n", pto->GetId());
         //   pto->fDisconnect = true;
         //   return true;
         //}
         //// In case there is a block that has been in flight from this peer for 2 + 0.5 * N times the block interval
         //// (with N the number of peers from which we're downloading validated blocks), disconnect due to timeout.
         //// We compensate for other peers to prevent killing off peers due to our own downstream link
         //// being saturated. We only count validated in-flight blocks so peers can't advertise non-existing block hashes
         //// to unreasonably increase our timeout.
         //if (state.vBlocksInFlight.size() > 0)
         //{
         //   QueuedBlock & queuedBlock = state.vBlocksInFlight.front();
         //   int nOtherPeersWithValidatedDownloads = nPeersWithValidatedDownloads - (state.nBlocksInFlightValidHeaders > 0);
         //   if (nNow > state.nDownloadingSince + consensusParams.nPowTargetSpacing * (BLOCK_DOWNLOAD_TIMEOUT_BASE + BLOCK_DOWNLOAD_TIMEOUT_PER_PEER * nOtherPeersWithValidatedDownloads))
         //   {
         //      LogPrintf("Timeout downloading block %s from peer=%d, disconnecting\n", queuedBlock.hash.ToString(), pto->GetId());
         //      pto->fDisconnect = true;
         //      return true;
         //   }
         //}

         return Task.CompletedTask;
      }

      /// <summary>
      /// Checks the fetchers score work, updating the <see cref="fetchersScore"/>.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      /// <returns></returns>
      private Task CheckFetchersScoreWork(CancellationToken cancellation)
      {
         using var writeFetcherLock = new WriteLock(this.fetcherSlimLock);

         uint min = 0, max = 0, sum = 0;
         foreach (var fetcher in this.fetchers)
         {
            var score = fetcher.GetScore();

            min = score < min ? score : min;
            max = score > max ? score : max;
            sum += score;
         }

         this.fetchersScore = (min, max, fetchers.Count == 0 ? 0 : (uint)(sum / fetchers.Count));

         return Task.CompletedTask;
      }

      public void RequireAssignment(IBlockFetcher fetcher, HeaderNode requestedBlock)
      {
         throw new NotImplementedException();
      }
   }
}
