using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Shards;

namespace MithrilShards.Logging.Serilog;

public class SerilogShard : IMithrilShard
{
   public ValueTask InitializeAsync(CancellationToken cancellationToken) => default;
   public ValueTask StartAsync(CancellationToken cancellationToken) => default;
   public ValueTask StopAsync(CancellationToken cancellationToken) => default;
}
