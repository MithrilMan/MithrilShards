namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever an incoming peer connection attempt failed.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionRejected : PeerEventBase
   {
      public string Reason { get; }

      public PeerConnectionRejected(IPeerContext peerContext, string reason) : base(peerContext)
      {
         Reason = reason;
      }
   }
}