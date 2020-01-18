using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerShard : IMithrilShard
   {
      public DevControllerShard() { }

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
