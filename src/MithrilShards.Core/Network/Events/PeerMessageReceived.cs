using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// A peer message has been received and parsed
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Events.PeerEventBase" />
   public class PeerMessageReceived : PeerEventBase
   {
      public INetworkMessage Message { get; }

      public PeerMessageReceived(IPeerContext peerContext, INetworkMessage message) : base(peerContext)
      {
         Message = message;
      }
   }
}