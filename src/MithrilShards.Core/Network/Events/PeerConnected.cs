namespace MithrilShards.Core.Network.Events;

/// <summary>
/// Event that is published whenever a peer connects to the node.
/// This happens prior to any Payload they have to exchange.
/// </summary>
/// <seealso cref="MithrilShards.Core.Network.Events.PeerEventBase" />
public class PeerConnected : PeerEventBase
{
   public PeerConnected(IPeerContext peerContext) : base(peerContext)
   {
   }
}
