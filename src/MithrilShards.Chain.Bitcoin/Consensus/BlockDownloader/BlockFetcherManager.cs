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
      /// The <see cref="_checkStaleBlockFetchersLoop"/> interval, in seconds.
      /// </summary>
      private const int CHECK_STALE_BLOCK_FETCHERS_LOOP_INTERVAL = 60;

      /// <summary>
      /// The <see cref="_checkFetchersScoreLoop"/> interval, in seconds.
      /// </summary>
      private const int CHECK_FETCHERS_SCORE_LOOP_INTERVAL = 60;

      /// <summary>
      /// The maximum number of blocks that can be fetched in parallel.
      /// </summary>
      private const int MAX_PARALLEL_DOWNLOADED_BLOCKS = 200;

      private static readonly object _channelLock = new object();
      private readonly ReaderWriterLockSlim _fetcherSlimLock = new ReaderWriterLockSlim();

      readonly ILogger<BlockFetcherManager> _logger;
      readonly IEventBus _eventBus;
      readonly IDateTimeProvider _dateTimeProvider;
      readonly IChainState _chainState;
      readonly IPeriodicWork _blockFetchAssignmentLoop;
      readonly IPeriodicWork _checkStaleBlockFetchersLoop;
      readonly IPeriodicWork _checkFetchersScoreLoop;

      /// <summary>
      /// The list of registered fetchers
      /// </summary>
      private readonly List<IBlockFetcher> _fetchers = new List<IBlockFetcher>();

      /// <summary>
      /// Contains the hashes of blocks we need to download because we have already their validated headers.
      /// </summary>
      private readonly ConcurrentQueue<HeaderNode> _blocksToDownload = new ConcurrentQueue<HeaderNode>();

      /// <summary>
      /// Contains the hashes of blocks we failed to fetch.
      /// This list is more important than <see cref="_blocksToDownload"/> because may refers to blocks
      /// closest to the tip
      /// </summary>
      private readonly ConcurrentDictionary<UInt256, HeaderNode> _failedBlockFetch = new ConcurrentDictionary<UInt256, HeaderNode>();

      /// <summary>
      /// The blocks we are actually downloading
      /// </summary>
      private readonly ConcurrentDictionary<IBlockFetcher, List<PendingDownload>> _blocksInDownload = new ConcurrentDictionary<IBlockFetcher, List<PendingDownload>>();

      /// <summary>
      /// Channel used to get data to send to fetchers.
      /// </summary>
      private readonly Channel<HeaderNode> _requiredBlocks;

      private readonly Dictionary<UInt256, (IBlockFetcher fetcher, List<QueuedBlock> queuedBlocks)> _mapBlocksInFlight = new Dictionary<UInt256, (IBlockFetcher, List<QueuedBlock>)>();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager _eventSubscriptionManager = new EventSubscriptionManager();

      /// <summary>
      /// The fetchers score values computed regularly by <see cref="_checkFetchersScoreLoop"/>
      /// </summary>
      private (uint min, uint max, uint average) _fetchersScore;

      public BlockFetcherManager(ILogger<BlockFetcherManager> logger,
                                 IEventBus eventBus,
                                 IDateTimeProvider dateTimeProvider,
                                 IChainState chainState,
                                 IPeriodicWork downloadAssignmentLoop,
                                 IPeriodicWork checkStaleBlockDownload,
                                 IPeriodicWork checkFetcherScore)
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._dateTimeProvider = dateTimeProvider;
         this._chainState = chainState;
         this._blockFetchAssignmentLoop = downloadAssignmentLoop;
         this._checkStaleBlockFetchersLoop = checkStaleBlockDownload;
         this._checkFetchersScoreLoop = checkFetcherScore;
         this._requiredBlocks = Channel.CreateUnbounded<HeaderNode>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

         this._blockFetchAssignmentLoop.Configure(stopOnException: false, exceptionHandler: this);
         this._checkStaleBlockFetchersLoop.Configure(stopOnException: false, exceptionHandler: this);
         this._checkFetchersScoreLoop.Configure(stopOnException: false, exceptionHandler: this);
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         // starts the loop that distribute block downloads requests.
         this._blockFetchAssignmentLoop.StartAsync(
            label: nameof(_blockFetchAssignmentLoop),
            work: BlockFetchAssignmentWorkAsync,
            interval: TimeSpan.Zero,
            cancellationToken
            );

         // starts the loop that check for stale block fetchers.
         this._checkStaleBlockFetchersLoop.StartAsync(
            label: nameof(_checkStaleBlockFetchersLoop),
            work: CheckStaleBlockFetchersWorkAsync,
            interval: TimeSpan.FromSeconds(CHECK_STALE_BLOCK_FETCHERS_LOOP_INTERVAL),
            cancellationToken
            );

         // starts the consumer loop that distribute block downloads requests.
         this._checkFetchersScoreLoop.StartAsync(
            label: nameof(_checkFetchersScoreLoop),
            work: CheckFetchersScoreWorkAsync,
            interval: TimeSpan.FromSeconds(CHECK_FETCHERS_SCORE_LOOP_INTERVAL),
            cancellationToken
            );

         // subscribe to events we are interested into
         _eventSubscriptionManager
            .RegisterSubscriptions(this._eventBus.Subscribe<BlockHeaderValidationSucceeded>(async (args) => await this.OnBlockHeaderValidationSucceededAsync(args).ConfigureAwait(false)))
            .RegisterSubscriptions(this._eventBus.Subscribe<BlockReceived>(this.OnBlockReceived));

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         this._logger.LogCritical("An unhandled exception has been raised in the {0} work.", failedWork.Label);
         feedback.ContinueExecution = false;
         feedback.IsCritical = true;
         feedback.Message = "Node may be unstable, restart the node to fix the problem";
      }

      public bool TryGetFetcher(UInt256 hash, [MaybeNullWhen(false)] out IBlockFetcher? fetcher)
      {
         bool result = this._mapBlocksInFlight.TryGetValue(hash, out (IBlockFetcher fetcher, List<QueuedBlock> queuedBlocks) items);
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
            if (!this._blocksInDownload.TryGetValue(arg.Fetcher, out List<PendingDownload>? pendingDownloads))
            {
               this._logger.LogDebug("Received block {UnrequestedBlock} from an unexpected source, do nothing.", blockHash);
               return;
            }

            //TODO: pendingDownloads can change during enumeration, so the ToList is used here to prevent that, could be better?
            PendingDownload? currentPending = pendingDownloads.ToList().FirstOrDefault(d => d.BlockInDownload.Hash == blockHash);

            if (currentPending != null)
            {
               pendingDownloads.Remove(currentPending);

               // TODO: check the time it took to download and update the score?
               long elapsedUsec = _dateTimeProvider.GetTimeMicros() - currentPending.StartingTime;
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
      private async Task OnBlockHeaderValidationSucceededAsync(BlockHeaderValidationSucceeded args)
      {
         HeaderNode currentNode = args.LastValidatedHeaderNode;

         if (args.NewHeadersFoundCount > 0)
         {
            //check if the header is relative to current "best header"
            //this.chainState.
            HeaderNode? bestNodeHeader = this._chainState.BestHeader;
            if (args.LastValidatedHeaderNode.LastCommonAncestor(bestNodeHeader) != bestNodeHeader)
            {
               /// these validated headers refers to a chain that's not currently the best chain so I don't require blocks yet.
               /// Blocks are requested when the new validated headers cause the bestNodeHeader to switch on a fork

               ///TODO: ensure we are not stuck in case our best known header refers to something not validated fully
               ///(e.g. headers validate but blocks are never sent to us)
               ///Technically this branch may be left without blocks and so should be removed and rolled back to be moved then to a better fork
               this._logger.LogDebug("Ignoring validated headers because they are on a lower fork at this time");
               return;
            }

            var newNodesToFetch = new List<HeaderNode>();

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

            foreach (HeaderNode? header in newNodesToFetch)
            {
               this._blocksToDownload.Enqueue(header);
            }
         }

         await Task.Delay(50).ConfigureAwait(false); // a delay introduced to let the peer adjust it's block availability

         DownloadBlocksIfPossible();
      }

      public void RegisterFetcher(IBlockFetcher blockFetcher)
      {
         using (new WriteLock(_fetcherSlimLock))
         {
            this._fetchers.Add(blockFetcher);
         }
      }

      /// <summary>
      /// try to enqueue blocks in case there are blocks to download and there are free download slots available.
      /// </summary>
      /// <returns></returns>
      private void DownloadBlocksIfPossible()
      {
         lock (_channelLock)
         {
            if (this._blocksToDownload.Count == 0)
            {
               this._logger.LogDebug("No blocks to download");
               // no more blocks to download
               return;
            }

            int availableSlots = MAX_PARALLEL_DOWNLOADED_BLOCKS - this._blocksInDownload.Count;

            for (int i = 0; i < availableSlots; i++)
            {
               if (!this._blocksToDownload.TryDequeue(out HeaderNode? blockToDownload))
               {
                  this._logger.LogTrace("No blocks to download");
                  // no more blocks to download
                  return;
               }

               this._requiredBlocks.Writer.TryWrite(blockToDownload);
            }
         }
      }

      /// <summary>
      /// The consumer that perform validation.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      private async Task BlockFetchAssignmentWorkAsync(CancellationToken cancellation)
      {
         await foreach (HeaderNode blockToDownload in this._requiredBlocks.Reader.ReadAllAsync(cancellation))
         {
            // get first 3 fetchers with score
            //this.fetchers
            //   .Where(f => f.GetFetchBlockScore(blockToDownload.Hash) > this.fetchersScore.average)
            //   .Take(3)
            //   .DefaultIfEmpty(this.fetchers.)

            //try to see if performance wise has too much impact
            IBlockFetcher? selectedFetcher = (
               from fetcher in this._fetchers
               let score = fetcher.GetFetchBlockScore(blockToDownload)
               orderby score descending
               where score > 0 // only peers that can fetch the block
               select fetcher
               )
               .FirstOrDefault((fetcher) => fetcher.TryFetchAsync(blockToDownload, 0).ConfigureAwait(false).GetAwaiter().GetResult()); //TODO use async

            if (selectedFetcher == null)
            {
               this._logger.LogDebug("None of the block fetcher is able to get block {BlockHash}, mark this block as failed", blockToDownload.Hash);
               this._failedBlockFetch.TryAdd(blockToDownload.Hash, blockToDownload);
               return;
            }

            if (!this._blocksInDownload.TryGetValue(selectedFetcher, out List<PendingDownload>? selectedFetcherPendingDownloads))
            {
               selectedFetcherPendingDownloads = new List<PendingDownload>();
               this._blocksInDownload.TryAdd(selectedFetcher, selectedFetcherPendingDownloads);
            }

            selectedFetcherPendingDownloads.Add(new PendingDownload(blockToDownload, selectedFetcher, this._dateTimeProvider.GetTimeMicros()));
         }
      }

      private Task CheckStaleBlockFetchersWorkAsync(CancellationToken cancellation)
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
      /// Checks the fetchers score work, updating the <see cref="_fetchersScore"/>.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      /// <returns></returns>
      private Task CheckFetchersScoreWorkAsync(CancellationToken cancellation)
      {
         using var writeFetcherLock = new WriteLock(this._fetcherSlimLock);

         uint min = 0, max = 0, sum = 0;
         foreach (IBlockFetcher? fetcher in this._fetchers)
         {
            uint score = fetcher.GetScore();

            min = score < min ? score : min;
            max = score > max ? score : max;
            sum += score;
         }

         this._fetchersScore = (min, max, _fetchers.Count == 0 ? 0 : (uint)(sum / _fetchers.Count));

         return Task.CompletedTask;
      }

      public void RequireAssignment(IBlockFetcher fetcher, HeaderNode requestedBlock)
      {
         throw new NotImplementedException();
      }
   }
}
