using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Forge
{
   /// <summary>
   /// Fake <see cref="IForgeConnectivity"/> implementation that acts as a placeholder and remember to the user assembling shards that
   /// a valid <see cref="IForgeConnectivity"/> implementation must be registered wit an instance of <see cref="IForgeBuilder"/>
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Forge.IForgeServer" />
   public sealed class FakeForgeConnectivity : IForgeConnectivity
   {
      const string error = "A valid concrete implementation of IForgeConnectivity must be registered on a IForgeBuilder.";

      public ValueTask AttemptConnectionAsync(EndPoint remoteEndPoint, CancellationToken cancellation)
      {
         throw new NotImplementedException();
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         throw new NotImplementedException(error);
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         throw new NotImplementedException(error);
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         throw new NotImplementedException(error);
      }
   }
}
