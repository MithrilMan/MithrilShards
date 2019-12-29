using System.Collections.Generic;

namespace MithrilShards.Core.Statistics {
   /// <summary>
   /// Define methods needed to search for and register statistic feeds provided by implementations of IStatisticFeedsProvider
   /// </summary>
   public interface IStatisticFeedsCollector {
      /// <summary>
      /// Registers the statistic feeds.
      /// </summary>
      /// <param name="statisticSource">The statistic source.</param>
      /// <param name="statisticfeeds">The feeds definition to register.</param>
      void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds);
   }
}
