using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Network {
   public class PeerContext : IPeerContext {
      /// <summary>
      /// Gets the direction of the peer connection.
      /// </summary>
      public PeerConnectionDirection Direction { get; }

      /// <summary>
      /// Gets the peer identifier.
      /// </summary>
      public string PeerId { get; }


      /// <summary>
      /// Gets the local peer end point.
      /// </summary>
      public IPEndPoint LocalEndPoint { get; }

      /// <summary>
      /// Gets the remote peer end point.
      /// </summary>
      public IPEndPoint RemoteEndPoint { get; }

      public PeerContext(PeerConnectionDirection direction, string peerId, EndPoint localEndPoint, EndPoint remoteEndPoint) {
         this.Direction = direction;
         this.PeerId = peerId;
         this.LocalEndPoint = localEndPoint.AsIPEndPoint();
         this.RemoteEndPoint = remoteEndPoint.AsIPEndPoint();
      }
   }
}
