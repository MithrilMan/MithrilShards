using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.Protocol.Processors
{
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public class NetworkMessageProcessorFactory : INetworkMessageProcessorFactory
   {
      readonly ILogger<INetworkMessageProcessorFactory> logger;
      readonly IServiceProvider serviceProvider;

      public NetworkMessageProcessorFactory(ILogger<INetworkMessageProcessorFactory> logger,
                                            IServiceProvider serviceProvider)
      {
         this.logger = logger;
         this.serviceProvider = serviceProvider;
      }

      /// <summary>
      /// Attaches known processors to specified peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      public async Task StartProcessorsAsync(IPeerContext peerContext)
      {
         if (peerContext is null)
         {
            ThrowHelper.ThrowArgumentNullException(nameof(peerContext));
         }

         IEnumerable<INetworkMessageProcessor> processors = this.serviceProvider.GetService<IEnumerable<INetworkMessageProcessor>>();
         foreach (INetworkMessageProcessor processor in processors)
         {
            // skip processors that aren't enabled
            if (!processor.Enabled) continue;

            peerContext.AttachNetworkMessageProcessor(processor);
            await processor.AttachAsync(peerContext).ConfigureAwait(false);
         }

         peerContext.Data.Set(new PeerNetworkMessageProcessorContainer(processors));
      }

      public async ValueTask ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
      {
         await peerContext.Data.Get<PeerNetworkMessageProcessorContainer>().ProcessMessageAsync(message, cancellation).ConfigureAwait(false);
      }
   }
}
