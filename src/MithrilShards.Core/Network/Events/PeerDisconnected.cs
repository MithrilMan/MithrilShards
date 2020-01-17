using System;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever a peer disconnects from the node.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerDisconnected : PeerEventBase
   {
      public string Reason { get; }

      public Exception? Exception { get; }

      public PeerDisconnected(IPeerContext peerContext, string reason, Exception? exception) : base(peerContext)
      {
         this.Reason = reason;
         this.Exception = exception;
      }
   }
}