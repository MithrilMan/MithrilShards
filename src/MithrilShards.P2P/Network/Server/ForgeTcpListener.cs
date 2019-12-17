using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MithrilShards.P2P.Network.Server {
   public class ForgeTcpListener : TcpListener {
      public ForgeTcpListener(IPEndPoint localEP) : base(localEP) {

         this.Server.LingerState = new LingerOption(true, 0);
         this.Server.NoDelay = true;
         this.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
         this.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      }

      public bool IsActive { get => base.Active; }
   }
}
