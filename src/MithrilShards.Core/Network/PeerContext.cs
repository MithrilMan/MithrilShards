using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Core.Network;

public class PeerContext(
   ILogger logger,
   IEventBus eventBus,
   PeerConnectionDirection direction,
   string peerId,
   EndPoint localEndPoint,
   EndPoint publicEndPoint,
   EndPoint remoteEndPoint,
   INetworkMessageWriter messageWriter
   ) : IPeerContext
{
   private readonly List<INetworkMessageProcessor> _messageProcessors = [];
   protected readonly ILogger logger = logger;
   protected readonly IEventBus eventBus = eventBus;
   protected readonly INetworkMessageWriter messageWriter = messageWriter;

   /// <summary>
   /// Gets the direction of the peer connection.
   /// </summary>
   public PeerConnectionDirection Direction { get; } = direction;

   /// <summary>
   /// Gets the peer identifier.
   /// </summary>
   public string PeerId { get; } = peerId;

   /// <summary>
   /// Gets the local peer end point.
   /// </summary>
   public IPEndPoint LocalEndPoint { get; } = localEndPoint.AsIPEndPoint();

   /// <summary>External IP address and port number used to access the node from external network.</summary>
   /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
   public IPEndPoint PublicEndPoint { get; } = publicEndPoint.AsIPEndPoint();

   /// <summary>
   /// Gets the remote peer end point.
   /// </summary>
   public IPEndPoint RemoteEndPoint { get; } = remoteEndPoint.AsIPEndPoint();

   public string? UserAgent { get; set; }

   public IFeatureCollection Features { get; } = new FeatureCollection();

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

   public bool IsConnected { get; protected set; } = false;

   public INetworkMessageWriter GetMessageWriter()
   {
      return messageWriter;
   }

   public virtual void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
   {
      if (_messageProcessors.Exists(p => p.GetType() == messageProcessor.GetType()))
      {
         throw new ArgumentException($"Cannot add multiple processors of the same type. Trying to attack {messageProcessor.GetType().Name} multiple times");
      }

      _messageProcessors.Add(messageProcessor);
   }

   public void Disconnect(string reason)
   {
      IsConnected = false;
      _ = eventBus.PublishAsync(new PeerDisconnectionRequired(RemoteEndPoint, reason));
   }

   public async ValueTask DisposeAsync()
   {
      logger.LogDebug("Disposing PeerContext of {PeerId}.", PeerId);
      foreach (INetworkMessageProcessor messageProcessor in _messageProcessors)
      {
         try
         {
            messageProcessor.Dispose();
         }
         catch (Exception ex)
         {
            logger.LogError(ex, "Fail to dispose message processor {MessageProcessor}", messageProcessor.GetType().Name);
         }
      }

      IsConnected = false;
      await ConnectionCancellationTokenSource.CancelAsync().ConfigureAwait(false);

      await eventBus.PublishAsync(new PeerDisconnected(this, "Client disconnected", null)).ConfigureAwait(false);
   }
}
