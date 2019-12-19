using System.Net.Sockets;
using System.Threading;

namespace MithrilShards.P2P.Network {
   public interface IPeerConnectionFactory {
      IPeerConnection CreatePeerConnection(TcpClient connectingPeer, CancellationToken cancellationToken);
   }
}