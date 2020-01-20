using System.Collections.Generic;

namespace MithrilShards.Dev.Controller.Models.Responses
{
   public class StatisticsGetAvailableFeeds
   {
      public string? FeedId { get; set; }
      public string? Title { get; set; }
      public IEnumerable<KeyValuePair<string, string>>? Fields { get; set; }
   }
}