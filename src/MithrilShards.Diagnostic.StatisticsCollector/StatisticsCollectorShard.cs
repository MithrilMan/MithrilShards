using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Shards;

namespace MithrilShards.Diagnostic.StatisticsCollector;

public class StatisticsCollectorShard(
   ILogger<StatisticsCollectorShard> logger,
   ConsoleKeyDumper? consoleKeyDumper = null)
   : IMithrilShard
{
   public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

   public Task StartAsync(CancellationToken cancellationToken)
   {
      logger.LogDebug("Starting StatisticsCollectorShard");

      if (consoleKeyDumper != null)
      {
         _ = consoleKeyDumper.StartListeningAsync();
      }

      return Task.CompletedTask;
   }

   public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
