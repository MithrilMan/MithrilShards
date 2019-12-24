using System;
using System.Net;

namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// Event that is published whenever a peer disconnects from the node.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerDisconnected : PeerEventBase {
      public PeerConnectionDirection Direction { get; }
      public string PeerId { get; }
      public string Reason { get; }

      public Exception Exception { get; }

      public PeerDisconnected(PeerConnectionDirection direction, string peerId, EndPoint localEndPoint, EndPoint remoteEndPoint, string reason, Exception exception) : base(localEndPoint, remoteEndPoint) {
         this.Direction = direction;
         this.PeerId = peerId;
         this.Reason = reason;
         this.Exception = exception;
      }
   }
}