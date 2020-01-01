﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Statistics;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Network {
   public class ConnectionManager : IConnectionManager, IStatisticFeedsProvider {
      private const string FEED_CONNECTED_PEERS = "ConnectedPeers";

      private readonly Dictionary<string, IPeerContext> inboundPeers = new Dictionary<string, IPeerContext>();
      private readonly Dictionary<string, IPeerContext> outboundPeers = new Dictionary<string, IPeerContext>();
      private readonly object subscriptionsLock = new object();

      private readonly ILogger<ConnectionManager> logger;
      private readonly IEventBus eventBus;
      readonly IStatisticFeedsCollector statisticFeedsCollector;
      readonly IEnumerable<IConnector> connectors;

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly List<SubscriptionToken> eventBusSubscriptionsTokens = new List<SubscriptionToken>();

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
                               ) {
         this.logger = logger;
         this.eventBus = eventBus;
         this.statisticFeedsCollector = statisticFeedsCollector;
         this.connectors = connectors;
      }

      /// <summary>
      /// Adds the specified peer to the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void AddConnectedPeer(PeerConnected @event) {
         Dictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? this.inboundPeers : this.outboundPeers;
         container[@event.PeerContext.PeerId] = @event.PeerContext;
         this.logger.LogDebug("Added peer {PeerId} to the list of connected peers", @event.PeerContext.PeerId);
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void RemoveConnectedPeer(PeerDisconnected @event) {
         Dictionary<string, IPeerContext> container = @event.PeerContext.Direction == PeerConnectionDirection.Inbound ? this.inboundPeers : this.outboundPeers;
         if (!container.Remove(@event.PeerContext.PeerId)) {
            this.logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else {
            this.logger.LogInformation("Peer {PeerId} disconnected.", @event.PeerContext.PeerId);
         }
      }

      public Task StartAsync(CancellationToken cancellationToken) {
         this.RegisterStatisticFeeds();
         lock (this.subscriptionsLock) {
            this.eventBusSubscriptionsTokens.Add(this.eventBus.Subscribe<PeerConnected>(this.AddConnectedPeer));
            this.eventBusSubscriptionsTokens.Add(this.eventBus.Subscribe<PeerDisconnected>(this.RemoveConnectedPeer));
         }

         // start the task that tries to connect to other peers
         _ = this.StartOutgoingConnectionAttempts(cancellationToken);

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken) {
         lock (this.subscriptionsLock) {
            foreach (SubscriptionToken token in this.eventBusSubscriptionsTokens) {
               token?.Dispose();
            }
            this.eventBusSubscriptionsTokens.Clear();
         }

         return Task.CompletedTask;
      }


      public string[] GetStatisticFeedValues(string feedId) {
         switch (feedId) {
            case FEED_CONNECTED_PEERS:
               return new string[] {
                  this.inboundPeers.Count.ToString(),
                  this.outboundPeers.Count.ToString()
               };
            default:
               return null;
         }
      }

      public void RegisterStatisticFeeds() {
         this.statisticFeedsCollector.RegisterStatisticFeeds(this,
            new StatisticFeedDefinition(
               FEED_CONNECTED_PEERS,
               "Connected Peers",
               new List<FieldDefinition>{
                  new FieldDefinition(
                     "Inbound",
                     "Number of inbound peers currently connected to one of the Forge listener",
                     15,
                     String.Empty
                     ),
                  new FieldDefinition(
                     "Outbound",
                     "Number of outbound peers our forge is currently connected to",
                     15,
                     String.Empty
                     )
               },
               TimeSpan.FromSeconds(15)
            )
         );
      }

      protected virtual async Task StartOutgoingConnectionAttempts(CancellationToken cancellation) {
         using IDisposable logger = this.logger.BeginScope("Starting Connectors");
         if (this.connectors == null) {
            this.logger.LogWarning("No Connectors found, the Forge will not try to connect to any peer.");
            return;
         }
         foreach (IConnector connector in this.connectors) {
            try {
               connector.StartConnectionLoopAsync(this, cancellation);
            }
            catch (OperationCanceledException Exception) {
               this.logger.LogDebug("Connector {Connector} canceled.", connector.GetType().Name);
            }
            catch (Exception ex) {
               this.logger.LogError(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", connector.GetType().Name);
            }
         }
      }

      public bool CanConnectTo(IPEndPoint endPoint) {

         // ensures I'm not already connected to the same endpoint
         if (this.outboundPeers.Values.ToList().Any(peer => peer.RemoteEndPoint.Equals(endPoint.EnsureIPv6()))) {
            this.logger.LogDebug("Already connected to peer {RemoteEndPoint}", endPoint);
            return false;
         }

         //TODO enhance this logic using a similar approach to IServerPeerConnectionGuards, but for clients
         return true;
      }
   }
}
