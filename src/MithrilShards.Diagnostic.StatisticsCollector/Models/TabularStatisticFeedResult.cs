using MithrilShards.Core.Statistics;

namespace MithrilShards.Diagnostic.StatisticsCollector.Models
{
   public class TabularStatisticFeedResult : StatisticFeedResult<TabularStatisticFeedResult>
   {
      public string Content { get; set; }

      internal TabularStatisticFeedResult(ScheduledStatisticFeed feed) : base(feed.StatisticFeedDefinition.FeedId, feed.LastResultsDate)
      {
         this.Content = feed.GetTabularFeed();
      }
   }
}