using System.Collections.Generic;

namespace MithrilShards.Core.Statistics
{
   /// <summary>
   /// Every component implementing this interface is exporting statistics relative to its job.
   /// </summary>
   public interface IStatisticFeedsProvider
   {
      /// <summary>
      /// Handy placeholder that feeds provider should use to Register a feed definition.
      /// </summary>
      /// <param name="statisticFeedsCollector">The statistic feeds collector used to register feeds.</param>
      /// <returns></returns>
      void RegisterStatisticFeeds();

      /// <summary>
      /// Gets the statistic feed values rows.
      /// </summary>
      /// <param name="feedId">The feed identifier.</param>
      /// <returns>The feed values, ordered by column definition</returns>
      List<object[]> GetStatisticFeedValues(string feedId);
   }
}
