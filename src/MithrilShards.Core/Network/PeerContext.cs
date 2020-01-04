using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Core.Network
{
   public class PeerContext : IPeerContext
   {
      private readonly List<INetworkMessageProcessor> messageProcessors = new List<INetworkMessageProcessor>();
      readonly ILogger logger;
      readonly INetworkMessageWriter messageWriter;

      /// <summary>
      /// Gets the direction of the peer connection.
      /// </summary>
      public PeerConnectionDirection Direction { get; }

      /// <summary>
      /// Gets the peer identifier.
      /// </summary>
      public string PeerId { get; }


      /// <summary>
      /// Gets the local peer end point.
      /// </summary>
      public IPEndPoint LocalEndPoint { get; }


      /// <summary>External IP address and port number used to access the node from external network.</summary>
      /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
      public IPEndPoint PublicEndPoint { get; }

      /// <summary>
      /// Gets the remote peer end point.
      /// </summary>
      public IPEndPoint RemoteEndPoint { get; }

      public IFeatureCollection Data { get; } = new FeatureCollection();

      /// <summary>
      /// Gets the version peers agrees to use when their respective version doesn't match.
      /// It should be the lower common version both parties implements.
      /// </summary>
      /// <value>
      /// The negotiated protocol version.
      /// </value>
      public virtual INegotiatedProtocolVersion NegotiatedProtocolVersion { get; } = new NegotiatedProtocolVersion();

      public PeerMetrics Metrics { get; } = new PeerMetrics();

      public CancellationTokenSource ConnectionCancellationTokenSource { get; } = new CancellationTokenSource();

      public PeerContext(ILogger logger,
                         PeerConnectionDirection direction,
                         string peerId,
                         EndPoint localEndPoint,
                         EndPoint publicEndPoint,
                         EndPoint remoteEndPoint,
                         INetworkMessageWriter messageWriter)
      {
         this.logger = logger;
         this.Direction = direction;
         this.PeerId = peerId;
         this.messageWriter = messageWriter;
         this.LocalEndPoint = localEndPoint.AsIPEndPoint();
         this.PublicEndPoint = publicEndPoint.AsIPEndPoint();
         this.RemoteEndPoint = remoteEndPoint.AsIPEndPoint();
      }

      public INetworkMessageWriter GetMessageWriter()
      {
         return this.messageWriter;
      }

      public virtual void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
      {
         if (this.messageProcessors.Exists(p => p.GetType() == messageProcessor.GetType()))
         {
            throw new ArgumentException($"Cannot add multiple processors of the same type. Trying to attack {messageProcessor.GetType().Name} multiple times");
         }

         this.messageProcessors.Add(messageProcessor);
      }

      public void Dispose()
      {
         this.logger.LogDebug("Disposing PeerContext of {PeerId}.", this.PeerId);
         foreach (INetworkMessageProcessor messageProcessor in this.messageProcessors)
         {
            messageProcessor.Dispose();
         }
      }

      public async Task ProcessMessageAsync(INetworkMessage message)
      {
         foreach (INetworkMessageProcessor messageProcessor in this.messageProcessors)
         {
            if (messageProcessor.Enabled)
            {
               if (!await messageProcessor.ProcessMessageAsync(message, default).ConfigureAwait(false))
               {
                  break;
               }
            }
         }
      }
   }
}
