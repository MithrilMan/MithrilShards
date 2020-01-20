using System.Collections.Generic;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Diagnostic.StatisticsCollector.Models
{
   public class RawStatisticFeedResult : StatisticFeedResult<RawStatisticFeedResult>
   {
      public List<FieldDefinition> Fields { get; }
      public List<string?[]> Values { get; }

      public RawStatisticFeedResult(ScheduledStatisticFeed feed) : base(feed.StatisticFeedDefinition.FeedId, feed.LastResultsDate)
      {
         this.Fields = feed.StatisticFeedDefinition.FieldsDefinition;
         this.Values = feed.lastResults;
      }
   }
}