using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MithrilShards.P2P.Network.Server.Guards {
   public struct ServerPeerConnectionGuardResult {
      /// <summary>
      /// Returns a successful guard check.
      /// </summary>
      /// <remarks>
      /// The value returned by this property corresponds to a passed guard execution.
      /// </remarks>
      public static ServerPeerConnectionGuardResult Success {
         get { return default(ServerPeerConnectionGuardResult); }
      }

      public bool IsDenied { get; private set; }

      public string DenyReason { get; private set; }

      public static ServerPeerConnectionGuardResult Allow() => Success;

      public static ServerPeerConnectionGuardResult Deny(string denyReason) {
         return new ServerPeerConnectionGuardResult {
            IsDenied = true,
            DenyReason = denyReason
         };
      }
   }
}