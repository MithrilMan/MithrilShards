﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.Protocol.Processors;

/// <summary>
/// Interfaces that define a generic network message
/// </summary>
public class NetworkMessageProcessorFactory : INetworkMessageProcessorFactory
{
   readonly ILogger<INetworkMessageProcessorFactory> _logger;
   readonly IServiceProvider _serviceProvider;

   public NetworkMessageProcessorFactory(ILogger<INetworkMessageProcessorFactory> logger,
                                         IServiceProvider serviceProvider)
   {
      _logger = logger;
      _serviceProvider = serviceProvider;
   }

   /// <summary>
   /// Attaches known processors to specified peer.
   /// </summary>
   /// <param name="peerContext">The peer context.</param>
   public async Task StartProcessorsAsync(IPeerContext peerContext)
   {
      ArgumentNullException.ThrowIfNull(peerContext, nameof(peerContext));

      IEnumerable<INetworkMessageProcessor> processors = _serviceProvider.GetService<IEnumerable<INetworkMessageProcessor>>()!;
      foreach (INetworkMessageProcessor processor in processors)
      {
         // skip processors that aren't enabled
         if (!processor.Enabled) continue;

         if (_logger.IsEnabled(LogLevel.Trace))
         {
            _logger.LogTrace("Attaching processor {ProcessorType} to peer {PeerId}", processor.GetType().Name, peerContext.PeerId);
         }

         peerContext.AttachNetworkMessageProcessor(processor);
         await processor.AttachAsync(peerContext).ConfigureAwait(false);
      }

      peerContext.Features.Set(new PeerNetworkMessageProcessorContainer(_serviceProvider.GetRequiredService<ILogger<PeerNetworkMessageProcessorContainer>>(), processors));
   }

   public async ValueTask ProcessMessageAsync(INetworkMessage message, IPeerContext peerContext, CancellationToken cancellation)
   {
      ArgumentNullException.ThrowIfNull(message, nameof(message));
      ArgumentNullException.ThrowIfNull(peerContext, nameof(peerContext));

      var container = peerContext.Features.Get<PeerNetworkMessageProcessorContainer>()
         ?? throw new InvalidOperationException("PeerNetworkMessageProcessorContainer not found in peer context.");

      await container.ProcessMessageAsync(message, cancellation).ConfigureAwait(false);
   }
}
