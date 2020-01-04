using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Network.Legacy.Server
{
   public interface IServerPeer
   {
      Task ListenAsync(CancellationToken cancellation);

      void StopListening();
   }
}
