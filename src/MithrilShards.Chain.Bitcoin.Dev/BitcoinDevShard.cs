using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Chain.Bitcoin.Dev
{
   /// <summary>
   /// Implemented as a shard in such a way that controllers will be discovered without explicitly load the assembly
   /// </summary>
   public class BitcoinDevShard : IMithrilShard
   {
      public ValueTask InitializeAsync(CancellationToken cancellationToken) => default;
      public ValueTask StartAsync(CancellationToken cancellationToken) => default;
      public ValueTask StopAsync(CancellationToken cancellationToken) => default;
   }
}