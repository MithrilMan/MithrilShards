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

      private readonly ILogger<ConnectionManager> _logger;
      private readonly IEventBus _eventBus;
      readonly IStatisticFeedsCollector _statisticFeedsCollector;
      readonly IEnumerable<IConnector> _connectors;

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
      public int ConnectedInboundPeersCount => this.inboundPeers.Count;

      /// <summary>
      /// Gets the connected outbound peers count.
      /// </summary>
      /// <value>
      /// The connected outbound peers count.
      /// </value>
      public int ConnectedOutboundPeersCount => this.outboundPeers.Count;


      public ConnectionManager(ILogger<ConnectionManager> logger,
                               IEventBus eventBus,
                               IStatisticFeedsCollector statisticFeedsCollector,
                               IEnumerable<IConnector> connectors
                               )
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._statisticFeedsCollector = statisticFeedsCollector;
         this._connectors = connectors;

         foreach (IConnector? connector in this._connectors)
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
         ConcurrentDictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? this.inboundPeers : this.outboundPeers;
         container[@event.PeerContext.PeerId] = @event.PeerContext;
         this._logger.LogDebug("Added peer {PeerId} to the list of connected peers", @event.PeerContext.PeerId);
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void RemoveConnectedPeer(PeerDisconnected @event)
      {
         ConcurrentDictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? this.inboundPeers : this.outboundPeers;
         if (!container.TryRemove(@event.PeerContext.PeerId, out _))
         {
            this._logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            this._logger.LogInformation("Peer {PeerId} disconnected.", @event.PeerContext.PeerId);
         }
      }

      public virtual Task StartAsync(CancellationToken cancellationToken)
      {
         this.RegisterStatisticFeeds();
         this._eventSubscriptionManager.RegisterSubscriptions(
               this._eventBus.Subscribe<PeerConnected>(this.AddConnectedPeer),
               this._eventBus.Subscribe<PeerDisconnected>(this.RemoveConnectedPeer),
               this._eventBus.Subscribe<PeerDisconnectionRequired>(this.OnPeerDisconnectionRequested)
         );

         // start the task that tries to connect to other peers
         _ = this.StartOutgoingConnectionAttemptsAsync(cancellationToken);

         return Task.CompletedTask;
      }

      public virtual Task StopAsync(CancellationToken cancellationToken)
      {
         this._eventSubscriptionManager.Dispose();

         return Task.CompletedTask;
      }

      public void RegisterStatisticFeeds()
      {
         string byteFormatter((object? value, int widthHint) item) => ByteSizeFormatter.HumanReadable((long)item.value!);

         this._statisticFeedsCollector.RegisterStatisticFeeds(this,
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
                     this.inboundPeers.Count,
                     this.outboundPeers.Count
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
         this._logger.LogDebug("Starting Connectors");
         if (this._connectors == null)
         {
            this._logger.LogWarning("No Connectors found, the Forge will not try to connect to any peer.");
            return Task.CompletedTask;
         }
         foreach (IConnector connector in this._connectors)
         {
            try
            {
               _ = connector.StartConnectionLoopAsync(cancellation);
            }
            catch (OperationCanceledException)
            {
               this._logger.LogDebug("Connector {Connector} canceled.", connector.GetType().Name);
            }
            catch (Exception ex)
            {
               this._logger.LogError(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", connector.GetType().Name);
            }
         }

         return Task.CompletedTask;
      }

      public bool CanConnectTo(IPEndPoint endPoint)
      {

         // ensures I'm not already connected to the same endpoint
         if (this.outboundPeers.Values.ToList().Any(peer => peer.RemoteEndPoint.Equals(endPoint.EnsureIPv6())))
         {
            this._logger.LogTrace("Already connected to peer {RemoteEndPoint}", endPoint);
            return false;
         }

         //TODO enhance this logic using a similar approach to IServerPeerConnectionGuards, but for clients
         return true;
      }

      protected void OnPeerDisconnectionRequested(PeerDisconnectionRequired @event)
      {
         IPEndPoint endPoint = @event.EndPoint.AsIPEndPoint().EnsureIPv6();
         IPeerContext peerContext = this.inboundPeers.Values
            .Concat(this.outboundPeers.Values.ToList())
            .FirstOrDefault(peer => peer.RemoteEndPoint.Equals(endPoint));

         if (peerContext != null)
         {
            this._logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection because: {DisconnectionReason}", endPoint, @event.Reason);
            peerContext.ConnectionCancellationTokenSource.Cancel();
         }
         else
         {
            this._logger.LogDebug("Requesting peer {RemoteEndPoint} disconnection failed, endpoint not matching with any connected peer.", endPoint);
         }
      }
   }
}
