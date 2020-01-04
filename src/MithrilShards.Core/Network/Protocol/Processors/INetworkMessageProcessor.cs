using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors
{
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public interface INetworkMessageProcessor : IDisposable
   {
      bool Enabled { get; }


      /// <summary>
      /// Processes the message asynchronously.
      /// Returns true if the flow must be stopped.
      /// </summary>
      /// <param name="message">The message.</param>
      /// <param name="cancellation">The cancellation token.</param>
      /// <returns></returns>
      ValueTask<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation);

      ValueTask AttachAsync(IPeerContext peerContext);
   }
}
