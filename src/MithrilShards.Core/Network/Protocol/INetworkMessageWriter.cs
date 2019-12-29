using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol {
   /// <summary>
   /// Interfaces that allow to send messages to another peer.
   /// </summary>
   public interface INetworkMessageWriter {
      ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default);

      ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default);
   }
}
