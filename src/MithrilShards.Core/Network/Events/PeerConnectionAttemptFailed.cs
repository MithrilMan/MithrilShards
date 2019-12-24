using System.Net;

namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// Event that is published whenever a peer connection attempt failed.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionAttemptFailed : PeerEventBase {
      public PeerConnectionDirection Direction { get; }

      public string Reason { get; }

      public PeerConnectionAttemptFailed(PeerConnectionDirection direction, EndPoint localEndPoint, EndPoint remoteEndPoint, string reason) : base(localEndPoint, remoteEndPoint) {
         this.Direction = direction;
         this.Reason = reason;
      }
   }
}