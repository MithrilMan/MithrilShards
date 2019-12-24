namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// Event that is published whenever the node tries to connect to a peer.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionAttempt : PeerEventBase {
      public PeerConnectionAttempt(IPeerContext peerContext) : base(peerContext) {
      }
   }
}