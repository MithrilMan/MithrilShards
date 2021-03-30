using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Network.Client;

namespace MithrilShards.Core.Network
{
   /// <summary>
   /// Manage the client connectivity (outgoing connections).
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public interface IForgeClientConnectivity
   {
      ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation);
   }
}