using System.Net;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// Requires a disconnection of the peer specified by the endpoint.
   /// Any component may request a peer disconnection publishing this event and specifying a reason.
   /// Depending on the peer type, the forge may decide to reconnect to the peer.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
   public class PeerDisconnectionRequired : EventBase
   {
      public EndPoint EndPoint { get; }
      public string Reason { get; }

      public PeerDisconnectionRequired(EndPoint endPoint, string reason)
      {
         this.EndPoint = endPoint;
         this.Reason = reason;
      }
   }
}
