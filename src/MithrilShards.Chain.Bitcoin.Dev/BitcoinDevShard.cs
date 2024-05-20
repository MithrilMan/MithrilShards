using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Shards;

namespace MithrilShards.Chain.Bitcoin.Dev;

/// <summary>
/// Implemented as a shard in such a way that controllers will be discovered without explicitly load the assembly
/// </summary>
public class BitcoinDevShard : IMithrilShard
{
   public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
   public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
   public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
