using System;
using MithrilShards.Core.Statistics;
using MithrilShards.Logging.ConsoleTableFormatter;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class ScheduledStatisticFeed
   {
      public IStatisticFeedsProvider Source { get; }

      public StatisticFeedDefinition StatisticFeedDefinition { get; }

      public DateTimeOffset NextPlannedExecution { get; set; }

      public TableBuilder? TableBuilder { get; set; } = null;

      public ScheduledStatisticFeed(IStatisticFeedsProvider source, StatisticFeedDefinition statisticFeedDefinition)
      {
         this.Source = source ?? throw new ArgumentNullException(nameof(source));
         this.StatisticFeedDefinition = statisticFeedDefinition ?? throw new ArgumentNullException(nameof(statisticFeedDefinition));
         this.NextPlannedExecution = DateTime.Now + statisticFeedDefinition.FrequencyTarget;
      }
   }
}
