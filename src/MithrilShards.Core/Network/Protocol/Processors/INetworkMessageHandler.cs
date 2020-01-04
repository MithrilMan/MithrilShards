using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors {
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public interface INetworkMessageHandler<TNetworkMessage> where TNetworkMessage : INetworkMessage {


      /// <summary>
      /// Processes the message asynchronously.
      /// Returns true if the flow must be stopped.
      /// </summary>
      /// <param name="message">The message.</param>
      /// <param name="cancellation">The cancellation token.</param>
      /// <returns></returns>
      ValueTask<bool> ProcessMessageAsync(TNetworkMessage message, CancellationToken cancellation);
   }
}
