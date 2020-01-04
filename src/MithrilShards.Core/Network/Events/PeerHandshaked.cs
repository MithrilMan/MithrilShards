namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever a Forge accomplishes the handshake with another peer.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerHandshaked : PeerEventBase
   {
      public PeerHandshaked(IPeerContext peerContext) : base(peerContext) { }
   }
}