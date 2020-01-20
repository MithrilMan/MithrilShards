using System;

namespace MithrilShards.Core.Statistics
{
   public interface IStatisticFeedResult
   {
      string FeedId { get; }

      DateTimeOffset Time { get; }
   }
}