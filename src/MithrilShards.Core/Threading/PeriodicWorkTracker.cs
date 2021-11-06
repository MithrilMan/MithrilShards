using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Core.Threading
{
   public class PeriodicWorkTracker : IPeriodicWorkTracker, IStatisticFeedsProvider
   {
      private const string FEED_PERIODIC_WORKS = "PeriodicWorks";

      readonly ILogger<PeriodicWorkTracker> _logger;
      readonly IStatisticFeedsCollector _statisticFeedsCollector;
      readonly ConcurrentDictionary<Guid, IPeriodicWork> _works = new();

      public PeriodicWorkTracker(ILogger<PeriodicWorkTracker> logger, IStatisticFeedsCollector statisticFeedsCollector)
      {
         _logger = logger;
         _statisticFeedsCollector = statisticFeedsCollector;

         RegisterStatisticFeeds();
      }

      public void StartTracking(IPeriodicWork work)
      {
         _works[work.Id] = work;
         _logger.LogDebug("Start tracking IPeriodicWork {IPeriodicWorkId} ({IPeriodicWorkLabel})", work.Id, work.Label);
      }

      public void StopTracking(IPeriodicWork work)
      {
         if (work is null)
         {
            ThrowHelper.ThrowArgumentNullException(nameof(work));
         }

         if (_works.TryRemove(work.Id, out IPeriodicWork? removedItem))
         {
            _logger.LogDebug("Stop tracking IPeriodicWork {IPeriodicWorkId} ({IPeriodicWorkLabel})", removedItem.Id, removedItem.Label);
         }
      }

      public void RegisterStatisticFeeds()
      {
         _statisticFeedsCollector.RegisterStatisticFeeds(this,
            new StatisticFeedDefinition(
               FEED_PERIODIC_WORKS,
               "Periodic Works",
               new List<FieldDefinition>{
                  new FieldDefinition(
                     "Id",
                     "Periodic work instance unique identifier",
                     36,
                     string.Empty
                     ),
                  new FieldDefinition(
                     "Label",
                     "Label assigned to the periodic work instance",
                     55,
                     string.Empty
                     ),
                  new FieldDefinition(
                     "Running",
                     "True if the work is running, false otherwise",
                     7,
                     string.Empty
                     ),
                  new FieldDefinition(
                     "Exceptions",
                     "Number of exceptions generated since work started",
                     10,
                     string.Empty
                     ),
                  new FieldDefinition(
                     "Last Exception",
                     "Last Exception message",
                     50,
                     string.Empty,
                     item => {
                        int maxLen = item.widthHint;
                        string message = (item.value as Exception)?.Message ?? string.Empty;

                        return message.Length >= maxLen ? $"{message.Substring(0, maxLen-3)}...": message;
                     }),
               },
               TimeSpan.FromSeconds(15)
            )
         );
      }

      public List<object?[]>? GetStatisticFeedValues(string feedId)
      {
         switch (feedId)
         {
            case FEED_PERIODIC_WORKS:
               return _works.Values
                  .ToList() //copy values
                  .Select(w => new object?[] {
                     w.Id,
                     w.Label,
                     w.IsRunning,
                     w.ExceptionsCount,
                     w.LastException
                  })
                  .ToList();
            default:
               return null;
         }
      }
   }
}
