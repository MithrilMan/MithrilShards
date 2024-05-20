using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Statistics;
using MithrilShards.Core.Utils;

namespace MithrilShards.Core.Network;

public class ConnectionManager : IConnectionManager, IStatisticFeedsProvider, IDisposable
{
   private const string FEED_CONNECTED_PEERS_SUMMARY = "ConnectedPeersSummary";
   private const string FEED_CONNECTED_PEERS = "ConnectedPeers";

   protected readonly ConcurrentDictionary<string, IPeerContext> inboundPeers = new();
   protected readonly ConcurrentDictionary<string, IPeerContext> outboundPeers = new();
   protected readonly HashSet<EndPoint> attemptingConnections = [];

   private readonly ILogger<ConnectionManager> _logger;
   private readonly IEventBus _eventBus;
   readonly IStatisticFeedsCollector _statisticFeedsCollector;
   readonly IEnumerable<IConnector> _connectors;

   private readonly object _connectionLock = new();

   /// <summary>
   /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
   /// Must be disposed to unregister subscriptions.
   /// </summary>
   private readonly EventSubscriptionManager _eventSubscriptionManager = new();

   /// <summary>
   /// Gets the connected inbound peers count.
   /// </summary>
   /// <value>
   /// The connected inbound peers count.
   /// </value>
   public int ConnectedInboundPeersCount => inboundPeers.Count;

   /// <summary>
   /// Gets the connected outbound peers count.
   /// </summary>
   /// <value>
   /// The connected outbound peers count.
   /// </value>
   public int ConnectedOutboundPeersCount => outboundPeers.Count;


   public ConnectionManager(ILogger<ConnectionManager> logger,
                            IEventBus eventBus,
                            IStatisticFeedsCollector statisticFeedsCollector,
                            IEnumerable<IConnector> connectors
                            )
   {
      _logger = logger;
      _eventBus = eventBus;
      _statisticFeedsCollector = statisticFeedsCollector;
      _connectors = connectors;

      foreach (IConnector? connector in _connectors)
      {
         connector?.SetConnectionManager(this);
      }
   }

   /// <summary>
   /// Adds the specified peer to the list of connected peer.
   /// </summary>
   /// <param name="event">The event.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns></returns>
   private ValueTask AddConnectedPeerAsync(PeerConnected @event, CancellationToken cancellationToken)
   {
      IPEndPoint ipEndPoint = @event.PeerContext.RemoteEndPoint.EnsureIPv6();
      if (@event.PeerContext.Direction == PeerConnectionDirection.Outbound)
      {
         lock (_connectionLock)
         {
            if (attemptingConnections.Remove(ipEndPoint))
            {
               _logger.LogDebug("EndPoint {RemoteEndPoint} Connected!. Removed from attemptingConnections list.", ipEndPoint);
            }
            else
            {
               _logger.LogDebug("EndPoint {RemoteEndPoint} Connected!. Not found in attemptingConnections list (shouldn't happen, need investigation)", ipEndPoint);
            }
         }
      }

      ConcurrentDictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? inboundPeers : outboundPeers;
      container[@event.PeerContext.PeerId] = @event.PeerContext;
      _logger.LogDebug("Connected to {RemoteEndPoint}, peer {PeerId} added to the list of connected peers.", ipEndPoint, @event.PeerContext.PeerId);

      return ValueTask.CompletedTask;
   }

   /// <summary>
   /// Removes the specified peer from the list of connected peer.
   /// </summary>
   /// <param name="event">The event.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   private ValueTask OnPeerDisconnectedAsync(PeerDisconnected @event, CancellationToken cancellationToken)
   {
      ConcurrentDictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? inboundPeers : outboundPeers;
      if (!container.TryRemove(@event.PeerContext.PeerId, out _))
      {
         _logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
      }
      else
      {
         _logger.LogDebug("Peer {PeerId} disconnected.", @event.PeerContext.PeerId);
      }

      return ValueTask.CompletedTask;
   }

   public virtual Task StartAsync(CancellationToken cancellationToken)
   {
      RegisterStatisticFeeds();
      _eventSubscriptionManager.RegisterSubscriptions(
            _eventBus.Subscribe<PeerConnected>(AddConnectedPeerAsync),
            _eventBus.Subscribe<PeerDisconnected>(OnPeerDisconnectedAsync),
            _eventBus.Subscribe<PeerDisconnectionRequired>(OnPeerDisconnectionRequestedAsync),
            _eventBus.Subscribe<PeerConnectionAttempt>(OnPeerConnectionAttemptAsync),
            _eventBus.Subscribe<PeerConnectionAttemptFailed>(OnPeerConnectionAttemptFailedAsync)
      );

      // start the task that tries to connect to other peers
      _ = StartOutgoingConnectionAttemptsAsync(cancellationToken);

      return Task.CompletedTask;
   }

   public virtual Task StopAsync(CancellationToken cancellationToken)
   {
      _eventSubscriptionManager.Dispose();

      return Task.CompletedTask;
   }

   public void RegisterStatisticFeeds()
   {
      static string byteFormatter((object? value, int widthHint) item) => ByteSizeFormatter.HumanReadable((long)item.value!);

      _statisticFeedsCollector.RegisterStatisticFeeds(this,
         new StatisticFeedDefinition(FEED_CONNECTED_PEERS_SUMMARY, "Connected Peers summary",
            [
               new FieldDefinition("Inbound","Number of inbound peers currently connected to one of the Forge listener",15),
               new FieldDefinition("Outbound","Number of outbound peers our forge is currently connected to",15)
            ],
            TimeSpan.FromSeconds(15)
         ),
         new StatisticFeedDefinition(FEED_CONNECTED_PEERS, "Connected Peers",
            [
               new FieldDefinition("Endpoint", "Peer remote endpoint", 25),
               new FieldDefinition("Type", "Type of connection (inbound, outbound, etc..)", 10),
               new FieldDefinition("Version", "Negotiated protocol version", 8),
               new FieldDefinition("User Agent", "Peer User Agent", 20),
               new FieldDefinition("Received", "Bytes received from this peer", 10, null, byteFormatter),
               new FieldDefinition("Sent", "Bytes sent to this peer", 10, null, byteFormatter),
               new FieldDefinition( "Wasted","Bytes that we received but wasn't understood from our node", 10, null, byteFormatter),
            ],
            TimeSpan.FromSeconds(15)
         )
      );
   }

   public List<object?[]>? GetStatisticFeedValues(string feedId)
   {
      return feedId switch
      {
         FEED_CONNECTED_PEERS_SUMMARY => [[inboundPeers.Count, outboundPeers.Count]],
         FEED_CONNECTED_PEERS => inboundPeers.Values.Concat(outboundPeers.Values)
            .Select(peer =>
               new object?[] {
                     peer.RemoteEndPoint,
                     peer.Direction,
                     peer.NegotiatedProtocolVersion.Version,
                     peer.UserAgent,
                     peer.Metrics.ReceivedBytes,
                     peer.Metrics.SentBytes,
                     peer.Metrics.WastedBytes,
               }
            ).ToList(),
         _ => null
      };
   }

   protected virtual Task StartOutgoingConnectionAttemptsAsync(CancellationToken cancellation)
   {
      _logger.LogDebug("Starting Connectors");
      if (_connectors == null)
      {
         _logger.LogWarning("No Connectors found, the Forge will not try to connect to any peer.");
         return Task.CompletedTask;
      }
      foreach (IConnector connector in _connectors)
      {
         try
         {
            _ = connector.StartConnectionLoopAsync(cancellation);
         }
         catch (OperationCanceledException)
         {
            _logger.LogDebug("Connector {Connector} canceled.", connector.GetType().Name);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", connector.GetType().Name);
         }
      }

      return Task.CompletedTask;
   }

   public bool CanConnectTo(IPEndPoint remoteEndPoint)
   {
      IPEndPoint ipEndPoint = remoteEndPoint.EnsureIPv6();
      lock (_connectionLock)
      {
         if (attemptingConnections.Contains(ipEndPoint))
         {
            _logger.LogDebug("A pending attempt to connect to {RemoteEndPoint} already exists", ipEndPoint);
            return false;
         }
      }

      // ensures I'm not already connected to the same endpoint
      if (outboundPeers.Values.ToList().Any(peer => peer.RemoteEndPoint.Equals(remoteEndPoint.EnsureIPv6())))
      {
         _logger.LogTrace("Already connected to peer {RemoteEndPoint}", remoteEndPoint);
         return false;
      }

      //TODO enhance this logic using a similar approach to IServerPeerConnectionGuards, but for clients
      return true;
   }

   protected async ValueTask OnPeerDisconnectionRequestedAsync(PeerDisconnectionRequired @event, CancellationToken cancellationToken)
   {
      IPEndPoint endPoint = @event.EndPoint.AsIPEndPoint().EnsureIPv6();
      IPeerContext? peerContext = inboundPeers.Values
         .Concat([.. outboundPeers.Values])
         .FirstOrDefault(peer => peer.RemoteEndPoint.Equals(endPoint));

      if (peerContext != null)
      {
         _logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection because: {DisconnectionReason}", endPoint, @event.Reason);
         await peerContext.ConnectionCancellationTokenSource.CancelAsync().ConfigureAwait(false);
      }
      else
      {
         _logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection failed, endpoint not matching with any connected peer.", endPoint);
      }
   }

   private ValueTask OnPeerConnectionAttemptAsync(PeerConnectionAttempt @event, CancellationToken cancellationToken)
   {
      IPEndPoint ipEndPoint = @event.RemoteEndPoint;
      lock (_connectionLock)
      {
         attemptingConnections.Add(ipEndPoint);
      }

      _logger.LogDebug("Connection attempt to {RemoteEndPoint}. Added to attemptingConnections list.", ipEndPoint);

      return ValueTask.CompletedTask;
   }

   private ValueTask OnPeerConnectionAttemptFailedAsync(PeerConnectionAttemptFailed @event, CancellationToken cancellationToken)
   {
      IPEndPoint ipEndPoint = @event.RemoteEndPoint.EnsureIPv6();
      lock (_connectionLock)
      {
         if (attemptingConnections.Remove(ipEndPoint))
         {
            _logger.LogDebug("EndPoint {RemoteEndPoint} removed from attemptingConnections list. {FailureReason}", ipEndPoint, @event.Reason);
         }
         else
         {
            _logger.LogWarning("EndPoint {RemoteEndPoint} not found in attemptingConnections list (shouldn't happen, need investigation). {FailureReason}", ipEndPoint, @event.Reason);
         }
      }

      return ValueTask.CompletedTask;
   }

   public void Dispose()
   {
      _eventSubscriptionManager.Dispose();
   }
}
