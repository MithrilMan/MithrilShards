using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Shards;

namespace MithrilShards.Dev.Controller;

internal class DevControllerShard : IMithrilShard
{
   public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

   /// <inheritdoc/>
   public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

   /// <inheritdoc/>
   public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
