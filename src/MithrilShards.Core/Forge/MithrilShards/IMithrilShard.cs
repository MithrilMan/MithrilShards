using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Forge.MithrilShards {
   public interface IMithrilShard {
      public Task InitializeAsync(CancellationToken cancellationToken);

      public Task StartAsync(CancellationToken cancellationToken);

      public Task StopAsync(CancellationToken cancellationToken);
   }
}