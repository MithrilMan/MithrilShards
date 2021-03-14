using System;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever a peer disconnects from the node.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Events.PeerEventBase" />
   public class PeerDisconnected : PeerEventBase
   {
      public string Reason { get; }

      public Exception? Exception { get; }

      public PeerDisconnected(IPeerContext peerContext, string reason, Exception? exception) : base(peerContext)
      {
         Reason = reason;
         Exception = exception;
      }
   }
}