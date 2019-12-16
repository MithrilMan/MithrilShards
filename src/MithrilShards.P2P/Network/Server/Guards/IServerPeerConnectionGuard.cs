using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public interface IServerPeerConnectionGuard {
      ServerPeerConnectionGuardResult Check(TcpClient tcpClient);
   }
}
