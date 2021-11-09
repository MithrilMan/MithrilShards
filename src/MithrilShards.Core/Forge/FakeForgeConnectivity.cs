using System;
using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;

namespace MithrilShards.Core.Forge;

/// <summary>
/// Fake <see cref="IForgeClientConnectivity" /> implementation that acts as a placeholder and remember to the user assembling shards that
/// a valid <see cref="IForgeClientConnectivity" /> implementation must be registered wit an instance of <see cref="IForgeBuilder" />
/// </summary>
/// <seealso cref="MithrilShards.Core.Network.IForgeClientConnectivity" />
public sealed class FakeForgeConnectivity : IForgeClientConnectivity
{
   const string ERROR = "A valid concrete implementation of IForgeConnectivity must be registered on a IForgeBuilder.";

   public ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation) => throw new NotImplementedException(ERROR);
}
