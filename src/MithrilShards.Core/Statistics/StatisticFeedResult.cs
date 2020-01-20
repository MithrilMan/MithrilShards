using System;

namespace MithrilShards.Core.Statistics
{
   public abstract class StatisticFeedResult<TStatisticResultType> : IStatisticFeedResult
      where TStatisticResultType : StatisticFeedResult<TStatisticResultType>
   {
      public string FeedId { get; }
      public DateTimeOffset Time { get; }

      public StatisticFeedResult(string feedId, DateTimeOffset time)
      {
         this.FeedId = feedId;
         this.Time = time;
      }
   }
}