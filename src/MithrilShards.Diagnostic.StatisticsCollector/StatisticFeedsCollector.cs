using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Statistics;
using MithrilShards.Diagnostic.StatisticsCollector.Models;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class StatisticFeedsCollector : IStatisticFeedsCollector
   {
      private readonly List<ScheduledStatisticFeed> _scheduledFeeds = new List<ScheduledStatisticFeed>();
      private readonly List<StatisticFeedDefinition> _registeredfeedDefinitions = new List<StatisticFeedDefinition>();
      private readonly ILogger<StatisticFeedsCollector> _logger;
      private readonly object _statisticsFeedsLock = new object();
      private readonly StringBuilder _logStringBuilder;
      private readonly StatisticsCollectorSettings _settings;

      public StatisticFeedsCollector(ILogger<StatisticFeedsCollector> logger, IOptions<StatisticsCollectorSettings> options)
      {
         _logger = logger;
         _logStringBuilder = new StringBuilder();
         _settings = options.Value;
      }

      public void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds)
      {
         lock (_statisticsFeedsLock)
         {
            if (statisticfeeds != null)
            {
               foreach (StatisticFeedDefinition feed in statisticfeeds)
               {
                  _registeredfeedDefinitions.Add(feed);
                  _scheduledFeeds.Add(new ScheduledStatisticFeed(statisticSource, feed));
               }
            }
         }
      }

      public IEnumerable<StatisticFeedDefinition> GetRegisteredFeedsDefinitions()
      {
         return _registeredfeedDefinitions.AsEnumerable();
      }

      /// <summary>
      /// Triggered when the application host is ready to start the service.
      /// If <see cref="settings.ContinuousConsoleDisplay"/> is true, statistics will be collected
      /// every <see cref="settings.ContinuousConsoleDisplayRate"/>, otherwise no statistics will be fetched automatically.
      /// </summary>
      /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
      /// <remarks>Changing <see cref="settings.ContinuousConsoleDisplay"/> at runtime won't affect automatically this behavior.</remarks>
      /// <returns></returns>
      public Task StartAsync(CancellationToken cancellationToken)
      {
         if (_settings.ContinuousConsoleDisplay)
         {
            _logger.LogDebug("Starting automatic feed collection every {ContinuousConsoleDisplayRate} seconds.", _settings.ContinuousConsoleDisplayRate);
            _ = StartFetchingLoopAsync(cancellationToken);
         }
         else
         {
            _logger.LogDebug("Automatic feed collection is disabled, no console output will happens.");
         }

         return Task.CompletedTask;
      }

      /// <summary>
      /// Starts the asynchronous fetching loop and console display of statistic feeds.
      /// </summary>
      /// <remarks>Can't use <see cref="Core.Threading.IPeriodicWork"/> because of circular references.</remarks>
      /// <param name="cancellationToken">The cancellation token.</param>
      public async Task StartFetchingLoopAsync(CancellationToken cancellationToken)
      {
         try
         {
            while (!cancellationToken.IsCancellationRequested)
            {
               FetchAllStatistics(false, true);

               if (_logStringBuilder.Length > 0)
               {
                  Console.WriteLine(_logStringBuilder.ToString());
                  _logStringBuilder.Clear();
               }

               await Task.Delay(TimeSpan.FromSeconds(_settings.ContinuousConsoleDisplayRate)).WithCancellationAsync(cancellationToken).ConfigureAwait(false);
            }
         }
         catch (OperationCanceledException)
         {
            // Task canceled, legit, ignoring exception.
         }
         catch (Exception)
         {
            _logger.LogDebug("Unexpected Exception during statistic generation, Statistic Collector stopped.");
         }
      }

      /// <summary>
      /// Fetches the statistics.
      /// </summary>
      /// <param name="forceFetch">
      /// If set to <c>true</c> forces fetching statistic even if the request is ahead of <see cref="ScheduledStatisticFeed.NextPlannedExecution"/>.
      /// </param>
      /// <param name="useTableBuilder">If set to <c>true</c> use the feed tableBuilder to build an human readable output.</param>
      private void FetchAllStatistics(bool forceFetch, bool useTableBuilder)
      {
         lock (_statisticsFeedsLock)
         {
            DateTimeOffset currentTime = DateTimeOffset.Now;
            foreach (ScheduledStatisticFeed feed in _scheduledFeeds)
            {
               if (forceFetch || feed.NextPlannedExecution <= currentTime)
               {
                  feed.NextPlannedExecution += feed.StatisticFeedDefinition.FrequencyTarget;

                  FetchFeedStatisticNoLock(feed, useTableBuilder);
               }
            }
         }
      }

      /// <summary>
      /// Fetches the feed statistic of a specific feed.
      /// </summary>
      /// <param name="feed">The feed.</param>
      /// <param name="feedDefinition">The feed definition.</param>
      /// <param name="useTableBuilder">If set to <c>true</c> use the feed tableBuilder to build an human readable output.</param>
      private void FetchFeedStatisticNoLock(ScheduledStatisticFeed feed, bool useTableBuilder)
      {
         StatisticFeedDefinition feedDefinition = feed.StatisticFeedDefinition;

         try
         {
            var newValues = new List<string?[]>();

            List<object?[]>? statisticValues = feed.Source.GetStatisticFeedValues(feedDefinition.FeedId);
            if (statisticValues != null)
            {
               foreach (object?[] values in statisticValues)
               {
                  newValues.Add(feedDefinition.FieldsDefinition
                     .Select((field, index) => field.ValueFormatter?.Invoke((value: values[index], widthHint: field.WidthHint)) ?? values[index]?.ToString())
                     .ToArray()
                     );
               }
            }

            if (useTableBuilder)
            {
               _logStringBuilder.AppendLine(feed.GetTabularFeed());
            }

            feed.SetLastResults(newValues);
         }
         catch (Exception ex)
         {
            _logger.LogDebug(ex, "Error generating statistics for {IStatisticFeedsProvider}", feed.Source.GetType().Name);
            throw;
         }
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      /// <summary>
      /// Gets an anonymous object containing the feeds dump.
      /// </summary>
      /// <returns></returns>
      public object GetFeedsDump()
      {
         FetchAllStatistics(true, false);

         lock (_statisticsFeedsLock)
         {
            return _scheduledFeeds.Select(feed => feed.GetLastResultsDump());
         }
      }

      /// <summary>
      /// Gets the specified feed dump.
      /// </summary>
      /// <param name="feedId">The feed identifier.</param>
      /// <param name="humanReadable">If set to <c>true</c> returns a human readable dump representation.</param>
      /// <returns></returns>
      public IStatisticFeedResult GetFeedDump(string feedId, bool humanReadable)
      {
         ScheduledStatisticFeed feed = _scheduledFeeds.Where(feed => feed.StatisticFeedDefinition.FeedId == feedId).FirstOrDefault();
         if (feed == null)
         {
            throw new ArgumentException("feedId not found");
         }

         lock (_statisticsFeedsLock)
         {
            FetchFeedStatisticNoLock(feed, humanReadable);
            if (humanReadable)
            {
               return new TabularStatisticFeedResult(feed);
            }
            else
            {
               return new RawStatisticFeedResult(feed);
            }
         }
      }
   }
}