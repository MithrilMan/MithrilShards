using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Shards;
using MithrilShards.Core.Network.Client;

namespace MithrilShards.Core.Network
{
   /// <summary>
   /// Manage the forge server, that allow incoming and outcoming connections.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public interface IForgeConnectivity : IMithrilShard
   {
      ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation);
   }
}
