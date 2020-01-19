using System;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Statistics
{
   /// <summary>
   /// Null implementation of IStatisticFeedsCollector.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Statistics.IStatisticFeedsCollector" />
   public class StatisticFeedsCollectorNullImplementation : IStatisticFeedsCollector
   {
      readonly ILogger<StatisticFeedsCollectorNullImplementation> logger;

      public StatisticFeedsCollectorNullImplementation(ILogger<StatisticFeedsCollectorNullImplementation> logger)
      {
         this.logger = logger;

         this.logger.LogWarning($"No statistic feed collector available, using {nameof(StatisticFeedsCollectorNullImplementation)}");
      }

      public void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds) { }
   }
}
