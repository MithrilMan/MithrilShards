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

namespace MithrilShards.Core.Network
{
   public class ConnectionManager : IConnectionManager, IStatisticFeedsProvider
   {
      private const string FEED_CONNECTED_PEERS_SUMMARY = "ConnectedPeersSummary";
      private const string FEED_CONNECTED_PEERS = "ConnectedPeers";

      protected readonly ConcurrentDictionary<string, IPeerContext> inboundPeers = new ConcurrentDictionary<string, IPeerContext>();
      protected readonly ConcurrentDictionary<string, IPeerContext> outboundPeers = new ConcurrentDictionary<string, IPeerContext>();
      protected readonly HashSet<EndPoint> attemptingConnections = new HashSet<EndPoint>();

      private readonly ILogger<ConnectionManager> _logger;
      private readonly IEventBus _eventBus;
      readonly IStatisticFeedsCollector _statisticFeedsCollector;
      readonly IEnumerable<IConnector> _connectors;

      private readonly object _connectionLock = new object();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// Must be disposed to unregister subscriptions.
      /// </summary>
      private readonly EventSubscriptionManager _eventSubscriptionManager = new EventSubscriptionManager();

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
      /// <param name="peerContext">The peer context.</param>
      private void AddConnectedPeer(PeerConnected @event)
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
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void OnPeerDisconnected(PeerDisconnected @event)
      {
         ConcurrentDictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? inboundPeers : outboundPeers;
         if (!container.TryRemove(@event.PeerContext.PeerId, out _))
         {
            _logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            _logger.LogInformation("Peer {PeerId} disconnected.", @event.PeerContext.PeerId);
         }
      }

      public virtual Task StartAsync(CancellationToken cancellationToken)
      {
         RegisterStatisticFeeds();
         _eventSubscriptionManager.RegisterSubscriptions(
               _eventBus.Subscribe<PeerConnected>(AddConnectedPeer),
               _eventBus.Subscribe<PeerDisconnected>(OnPeerDisconnected),
               _eventBus.Subscribe<PeerDisconnectionRequired>(OnPeerDisconnectionRequested),
               _eventBus.Subscribe<PeerConnectionAttempt>(OnPeerConnectionAttempt),
               _eventBus.Subscribe<PeerConnectionAttemptFailed>(OnPeerConnectionAttemptFailed)
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
         string byteFormatter((object? value, int widthHint) item) => ByteSizeFormatter.HumanReadable((long)item.value!);

         _statisticFeedsCollector.RegisterStatisticFeeds(this,
            new StatisticFeedDefinition(FEED_CONNECTED_PEERS_SUMMARY, "Connected Peers summary",
               new List<FieldDefinition>{
                  new FieldDefinition("Inbound","Number of inbound peers currently connected to one of the Forge listener",15),
                  new FieldDefinition("Outbound","Number of outbound peers our forge is currently connected to",15)
               },
               TimeSpan.FromSeconds(15)
            ),
            new StatisticFeedDefinition(FEED_CONNECTED_PEERS, "Connected Peers",
               new List<FieldDefinition>{
                  new FieldDefinition("Endpoint", "Peer remote endpoint", 25),
                  new FieldDefinition("Type", "Type of connection (inbound, outbound, etc..)", 10),
                  new FieldDefinition("Version", "Negotiated protocol version", 8),
                  new FieldDefinition("User Agent", "Peer User Agent", 20),
                  new FieldDefinition("Received", "Bytes received from this peer", 10, null, byteFormatter),
                  new FieldDefinition("Sent", "Bytes sent to this peer", 10, null, byteFormatter),
                  new FieldDefinition( "Wasted","Bytes that we received but wasn't understood from our node", 10, null, byteFormatter),
               },
               TimeSpan.FromSeconds(15)
            )
         );
      }

      public List<object?[]>? GetStatisticFeedValues(string feedId)
      {
         return feedId switch
         {
            FEED_CONNECTED_PEERS_SUMMARY => new List<object?[]> {
                  new object?[] {
                     inboundPeers.Count,
                     outboundPeers.Count
                  }
               },
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

      protected void OnPeerDisconnectionRequested(PeerDisconnectionRequired @event)
      {
         IPEndPoint endPoint = @event.EndPoint.AsIPEndPoint().EnsureIPv6();
         IPeerContext peerContext = inboundPeers.Values
            .Concat(outboundPeers.Values.ToList())
            .FirstOrDefault(peer => peer.RemoteEndPoint.Equals(endPoint));

         if (peerContext != null)
         {
            _logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection because: {DisconnectionReason}", endPoint, @event.Reason);
            peerContext.ConnectionCancellationTokenSource.Cancel();
         }
         else
         {
            _logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection failed, endpoint not matching with any connected peer.", endPoint);
         }
      }

      private void OnPeerConnectionAttempt(PeerConnectionAttempt @event)
      {
         IPEndPoint ipEndPoint = @event.RemoteEndPoint;
         lock (_connectionLock)
         {
            attemptingConnections.Add(ipEndPoint);
         }

         _logger.LogDebug("Connection attempt to {RemoteEndPoint}. Added to attemptingConnections list.", ipEndPoint);
      }

      private void OnPeerConnectionAttemptFailed(PeerConnectionAttemptFailed @event)
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
      }
   }
}
