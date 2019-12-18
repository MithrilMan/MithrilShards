using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.P2P.Network {
   public interface IPeerConnectionFactory {
      Task AcceptConnectionAsync(TcpClient connectingPeer, CancellationToken cancellationToken);
   }
}