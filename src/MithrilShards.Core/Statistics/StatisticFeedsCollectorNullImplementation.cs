using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Statistics;

/// <summary>
/// Null implementation of IStatisticFeedsCollector.
/// </summary>
/// <seealso cref="MithrilShards.Core.Statistics.IStatisticFeedsCollector" />
public class StatisticFeedsCollectorNullImplementation : IStatisticFeedsCollector
{
   private class NullStatisticFeedResult : StatisticFeedResult<NullStatisticFeedResult>
   {
      public NullStatisticFeedResult() : base(string.Empty, DateTimeOffset.Now) { }
   }

   readonly ILogger<StatisticFeedsCollectorNullImplementation> _logger;

   public StatisticFeedsCollectorNullImplementation(ILogger<StatisticFeedsCollectorNullImplementation> logger)
   {
      _logger = logger;

      _logger.LogWarning($"No statistic feed collector available, using {nameof(StatisticFeedsCollectorNullImplementation)}");
   }

   public IStatisticFeedResult GetFeedDump(string feedId, bool humanReadable)
   {
      return new NullStatisticFeedResult();
   }

   public object GetFeedsDump()
   {
      return new object();
   }

   public IEnumerable<StatisticFeedDefinition> GetRegisteredFeedsDefinitions()
   {
      return Enumerable.Empty<StatisticFeedDefinition>();
   }

   public void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds) { }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      throw new NotImplementedException("Don't register this class as IHostedService, it's a fake implementation");
   }

   public Task StopAsync(CancellationToken cancellationToken)
   {
      throw new NotImplementedException("Don't register this class as IHostedService, it's a fake implementation");
   }
}
