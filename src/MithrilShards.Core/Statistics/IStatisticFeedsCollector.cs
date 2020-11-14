using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Statistics
{
   /// <summary>
   /// Define methods needed to search for and register statistic feeds provided by implementations of IStatisticFeedsProvider
   /// </summary>
   public interface IStatisticFeedsCollector: IHostedService
   {
      /// <summary>
      /// Registers the statistic feeds.
      /// </summary>
      /// <param name="statisticSource">The statistic source.</param>
      /// <param name="statisticfeeds">The feeds definition to register.</param>
      void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds);

      /// <summary>
      /// Gets the registered feeds definitions.
      /// </summary>
      /// <returns></returns>
      IEnumerable<StatisticFeedDefinition> GetRegisteredFeedsDefinitions();

      /// <summary>
      /// Gets an anonymous object containing the feeds dump.
      /// </summary>
      object GetFeedsDump();

      /// <summary>
      /// Gets the specified feed dump.
      /// </summary>
      /// <param name="feedId">The feed identifier.</param>
      /// <param name="humanReadable">If set to <c>true</c> returns a human readable dump representation.</param>
      /// <returns></returns>
      IStatisticFeedResult GetFeedDump(string feedId, bool humanReadable);
   }
}
