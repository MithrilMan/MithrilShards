using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Diagnostic.StatisticsCollector {
   public class StatisticsCollectorShard : IMithrilShard {
      readonly ILogger<StatisticsCollectorShard> logger;

      public StatisticsCollectorShard(ILogger<StatisticsCollectorShard> logger) {
         this.logger = logger;
      }

      public Task InitializeAsync(CancellationToken cancellationToken) {
         return Task.CompletedTask;
      }

      public Task StartAsync(CancellationToken cancellationToken) {
         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken) {
         return Task.CompletedTask;
      }
   }
}
