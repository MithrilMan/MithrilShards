namespace MithrilShards.Core.Network.Server.Guards
{
   public class ServerPeerConnectionGuardResult
   {
      /// <summary>
      /// Returns a successful guard check.
      /// </summary>
      /// <remarks>
      /// The value returned by this property corresponds to a passed guard execution.
      /// </remarks>
      public static ServerPeerConnectionGuardResult Success { get; } = new ServerPeerConnectionGuardResult { IsDenied = false };

      public bool IsDenied { get; private set; }

      public string DenyReason { get; private set; } = string.Empty;

      public static ServerPeerConnectionGuardResult Allow() => Success;

      public static ServerPeerConnectionGuardResult Deny(string denyReason)
      {
         return new ServerPeerConnectionGuardResult
         {
            IsDenied = true,
            DenyReason = denyReason ?? string.Empty
         };
      }
   }
}