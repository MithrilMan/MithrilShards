using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Fake <see cref="IForgeServer"/> implementation that acts as a placeholder and remember to the user assembling shards that
   /// a valid IForgeServer implementation must be registered wit an instance of <see cref="IForgeBuilder"/>
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Forge.IForgeServer" />
   public sealed class FakeForgeServer : IForgeServer {
      const string error = "A valid instance of ForgeServer must be registered on a IForgeBuilder.";

      public Task InitializeAsync(CancellationToken cancellationToken) {
         throw new NotImplementedException(error);
      }

      public Task StartAsync(CancellationToken cancellationToken) {
         throw new NotImplementedException(error);
      }

      public Task StopAsync(CancellationToken cancellationToken) {
         throw new NotImplementedException(error);
      }
   }
}
