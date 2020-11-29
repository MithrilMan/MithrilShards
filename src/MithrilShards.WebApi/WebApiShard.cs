using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.WebApi
{
   public class WebApiShard : IMithrilShard
   {
      public ValueTask InitializeAsync(CancellationToken cancellationToken) => default;
      public ValueTask StartAsync(CancellationToken cancellationToken) => default;
      public ValueTask StopAsync(CancellationToken cancellationToken) => default;
   }
}
