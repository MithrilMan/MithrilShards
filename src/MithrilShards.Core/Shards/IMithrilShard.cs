using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Shards
{
   public interface IMithrilShard
   {
      public ValueTask InitializeAsync(CancellationToken cancellationToken);

      public ValueTask StartAsync(CancellationToken cancellationToken);

      public ValueTask StopAsync(CancellationToken cancellationToken);
   }
}