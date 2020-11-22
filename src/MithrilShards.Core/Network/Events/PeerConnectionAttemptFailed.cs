namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever a peer connection attempt failed.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionAttemptFailed : PeerEventBase
   {
      public string Reason { get; }

      public PeerConnectionAttemptFailed(IPeerContext peerContext, string reason) : base(peerContext)
      {
         Reason = reason;
      }
   }
}