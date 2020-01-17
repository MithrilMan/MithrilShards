using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge
{
   /// <summary>
   /// Manage the forge server, that allow incoming and outcoming connections.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public interface IForgeConnectivity : IMithrilShard
   {
      Task AttemptConnectionAsync(EndPoint remoteEndPoint, CancellationToken cancellation);
   }
}
