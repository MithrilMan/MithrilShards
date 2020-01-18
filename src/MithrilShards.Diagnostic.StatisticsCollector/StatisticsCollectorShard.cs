using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class StatisticsCollectorShard : IMithrilShard
   {
      public StatisticsCollectorShard() { }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         return default;
      }
   }
}
