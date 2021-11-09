using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Core.Network.Events;

/// <summary>
/// A peer message has been sent successfully.
/// </summary>
/// <seealso cref="MithrilShards.Core.Network.Events.PeerEventBase" />
public class PeerMessageSent : PeerEventBase
{
   /// <summary>
   /// Gets the sent message.
   /// </summary>
   public INetworkMessage Message { get; }

   /// <summary>
   /// Gets the raw size of the message, in bytes.
   /// </summary>
   public int Size { get; }

   public PeerMessageSent(IPeerContext peerContext, INetworkMessage message, int size) : base(peerContext)
   {
      Message = message;
      Size = size;
   }
}
