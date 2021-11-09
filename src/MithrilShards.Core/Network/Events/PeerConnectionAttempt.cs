using System.Net;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Network.Events;

/// <summary>
/// Event that is published whenever the node tries to connect to a peer.
/// </summary>
/// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
public class PeerConnectionAttempt : EventBase
{
   public IPEndPoint RemoteEndPoint { get; }
   public PeerConnectionAttempt(IPEndPoint remoteEndPoint)
   {
      RemoteEndPoint = remoteEndPoint;
   }
}
