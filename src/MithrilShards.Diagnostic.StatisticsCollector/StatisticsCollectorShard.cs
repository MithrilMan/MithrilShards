using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class StatisticsCollectorShard : IMithrilShard
   {
      readonly ILogger<StatisticsCollectorShard> _logger;
      readonly ConsoleKeyDumper? _consoleKeyDumper;

      public StatisticsCollectorShard(ILogger<StatisticsCollectorShard> logger, ConsoleKeyDumper? consoleKeyDumper = null)
      {
         _logger = logger;
         _consoleKeyDumper = consoleKeyDumper;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         if (_consoleKeyDumper != null)
         {
            _ = _consoleKeyDumper.StartListening();
         }

         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         return default;
      }
   }
}
