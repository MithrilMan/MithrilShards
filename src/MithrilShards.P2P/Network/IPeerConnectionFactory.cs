using System.Net.Sockets;
using System.Threading;

namespace MithrilShards.Network.Network {
   public interface IPeerConnectionFactory {
      IPeerConnection CreatePeerConnection(TcpClient connectingPeer, CancellationToken cancellationToken);
   }
}