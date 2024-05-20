using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Shards;

namespace MithrilShards.Example;

public class ExampleShard : IMithrilShard
{

   public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

   public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

   public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
