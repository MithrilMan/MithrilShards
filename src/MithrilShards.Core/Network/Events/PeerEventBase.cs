using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// Base peer event.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public abstract class PeerEventBase : EventBase {
      public IPeerContext PeerContext { get; }

      public PeerEventBase(IPeerContext peerContext) {
         this.PeerContext = peerContext;
      }
   }
}