using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors;

/// <summary>
/// Interfaces that define a generic network message
/// </summary>
public interface INetworkMessageProcessorFactory
{

   /// <summary>
   /// Starts the processors attaching them to <paramref name="peerContext"/>.
   /// </summary>
   /// <param name="peerContext">The peer context.</param>
   Task StartProcessorsAsync(IPeerContext peerContext);

   /// <summary>
   /// Processes the message asynchronously, calling all <see cref="INetworkMessageProcessor" /> instances that have to handle current message type.
   /// </summary>
   /// <param name="message">The network message.</param>
   /// <param name="peerContext">The peer context.</param>
   /// <param name="cancellation">The cancellation.</param>
   /// <returns></returns>
   ValueTask ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation);
}
