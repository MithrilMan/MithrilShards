using System.Net;

namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// Event that is published whenever the node tries to connect to a peer.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionAttempt : PeerEventBase {
      public PeerConnectionDirection Direction { get; }

      public PeerConnectionAttempt(PeerConnectionDirection direction, EndPoint localEndPoint, EndPoint remoteEndPoint) : base(localEndPoint, remoteEndPoint) {
         this.Direction = direction;
      }
   }
}