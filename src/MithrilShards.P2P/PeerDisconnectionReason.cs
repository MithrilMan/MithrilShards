using System;

namespace MithrilShards.Network.Legacy {
   /// <summary>
   /// Explanation of why a peer has been disconnected.
   /// </summary>
   public class PeerDisconnectionReason {
      /// <summary>Explicative reason of peer disconnection.</summary>
      public string Reason { get; set; }

      /// <summary>Exception that caused the peer disconnection, or <c>null</c> if there were no exceptions.</summary>
      public Exception Exception { get; set; }
   }
}
