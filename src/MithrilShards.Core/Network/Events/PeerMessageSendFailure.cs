using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Core.Network.Events
{
   /// <summary>
   /// A peer message failed to be sent.
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerMessageSendFailure : PeerEventBase
   {
      /// <summary>
      /// The failed message. Can be null if the exception was caused during the Message creation.
      /// </value>
      public INetworkMessage Message { get; }

      public System.Exception Exception { get; }

      public PeerMessageSendFailure(IPeerContext peerContext, INetworkMessage message, System.Exception exception) : base(peerContext)
      {
         Message = message;
         Exception = exception;
      }
   }
}