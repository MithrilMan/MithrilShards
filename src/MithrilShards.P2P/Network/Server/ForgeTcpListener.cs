using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MithrilShards.Network.Network.Server {
   public class ForgeTcpListener : TcpListener {

      public bool IsActive { get => base.Active; }

      /// <summary>Address of the end point the client is connected to, or <c>null</c> if the client has not connected yet.</summary>
      public IPEndPoint RemoteEndPoints {
         get {
            return (IPEndPoint)this.RemoteEndPoints;
         }
      }

      public ForgeTcpListener(IPEndPoint localEndPoint) : base(localEndPoint) {
         this.Server.LingerState = new LingerOption(true, 0);
         this.Server.NoDelay = true;
         this.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
         this.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      }
   }
}
