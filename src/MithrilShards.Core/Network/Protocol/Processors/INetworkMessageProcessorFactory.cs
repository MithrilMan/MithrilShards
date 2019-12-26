using System;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors {
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public interface INetworkMessageProcessorFactory {

      /// <summary>
      /// Starts the processors attaching them to <paramref name="peerContext"/>.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      void StartProcessors(IPeerContext peerContext);
   }
}
