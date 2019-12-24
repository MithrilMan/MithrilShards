using System.Net;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Network.Events {
    /// <summary>
    /// Base peer event.
    /// </summary>
    /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
    public abstract class PeerEventBase : EventBase
    {
      /// <summary>
      /// Gets the local peer end point.
      /// </summary>
      public IPEndPoint LocalEndPoint { get; }

      /// <summary>
      /// Gets the remote peer end point.
      /// </summary>
      public IPEndPoint RemoteEndPoint { get; }

        public PeerEventBase(EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
         this.LocalEndPoint = localEndPoint.AsIPEndPoint();
         this.RemoteEndPoint = remoteEndPoint.AsIPEndPoint();
        }
    }
}