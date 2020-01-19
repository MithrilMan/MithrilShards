using System;
using System.Collections.Generic;
using MithrilShards.Core.Statistics;
using MithrilShards.Logging.ConsoleTableFormatter;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class ScheduledStatisticFeed
   {
      /// <summary>
      /// Gets the source of the feed.
      /// </summary>
      /// <value>
      /// The source.
      /// </value>
      public IStatisticFeedsProvider Source { get; }

      /// <summary>
      /// Gets the statistic feed definition.
      /// </summary>
      /// <value>
      /// The statistic feed definition.
      /// </value>
      public StatisticFeedDefinition StatisticFeedDefinition { get; }

      /// <summary>
      /// Gets the next planned execution time.
      /// </summary>
      /// <value>
      /// The next planned execution.
      /// </value>
      public DateTimeOffset NextPlannedExecution { get; internal set; }

      /// <summary>
      /// Gets the table builder used to generate a tabular output.
      /// </summary>
      /// <value>
      /// The table builder.
      /// </value>
      public TableBuilder? TableBuilder { get; internal set; } = null;

      /// <summary>
      /// Gets or sets the last obtained result.
      /// </summary>
      /// <value>
      /// The last obtained result.
      /// </value>
      public List<string?[]> LastResults { get; } = new List<string?[]>();

      public ScheduledStatisticFeed(IStatisticFeedsProvider source, StatisticFeedDefinition statisticFeedDefinition)
      {
         this.Source = source ?? throw new ArgumentNullException(nameof(source));
         this.StatisticFeedDefinition = statisticFeedDefinition ?? throw new ArgumentNullException(nameof(statisticFeedDefinition));
         this.NextPlannedExecution = DateTime.Now + statisticFeedDefinition.FrequencyTarget;
      }
   }
}
