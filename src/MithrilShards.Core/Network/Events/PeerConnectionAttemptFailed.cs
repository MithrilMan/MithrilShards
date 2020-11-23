using System.Net;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Event that is published whenever an outgoing peer connection attempt failed.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerConnectionAttemptFailed : EventBase
   {
      public IPEndPoint RemoteEndPoint { get; }

      public string Reason { get; }

      public PeerConnectionAttemptFailed(IPEndPoint remoteEndPoint, string reason)
      {
         RemoteEndPoint = remoteEndPoint;
         Reason = reason;
      }
   }
}