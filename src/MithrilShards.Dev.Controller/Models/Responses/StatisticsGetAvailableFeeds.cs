using System.Collections.Generic;

namespace MithrilShards.Dev.Controller.Models.Responses;

public class StatisticsGetAvailableFeeds
{
   public class StatisticFeedField
   {
      public string? Label { get; set; }
      public string? Description { get; set; }
   }

   public string? FeedId { get; set; }
   public string? Title { get; set; }
   public IEnumerable<StatisticFeedField>? Fields { get; set; }
}
