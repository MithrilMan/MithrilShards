using System.Net.Sockets;
using System.Threading;

namespace MithrilShards.Network.Legacy
{
   public interface IPeerConnectionFactory
   {
      IPeerConnection CreatePeerConnection(TcpClient connectingPeer, CancellationToken cancellationToken);
   }
}