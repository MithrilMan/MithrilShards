using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.PeerAddressBook;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Core.Network.PeerBehaviorManager
{
   public partial class DefaultPeerBehaviorManager : IPeerBehaviorManager, IDisposable
   {
      private const int INITIAL_SCORE = 0;
      private readonly ILogger<DefaultPeerBehaviorManager> logger;
      private readonly IEventBus eventBus;
      private readonly ForgeConnectivitySettings connectivitySettings;
      private readonly IPeerAddressBook peerAddressBook;

      private readonly Dictionary<string, PeerScore> connectedPeers = new Dictionary<string, PeerScore>();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager eventSubscriptionManager = new EventSubscriptionManager();

      public DefaultPeerBehaviorManager(ILogger<DefaultPeerBehaviorManager> logger,
                                        IEventBus eventBus,
                                        IStatisticFeedsCollector statisticFeedsCollector,
                                        IOptions<ForgeConnectivitySettings> connectivityOptions,
                                        IPeerAddressBook peerAddressBook)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.statisticFeedsCollector = statisticFeedsCollector;
         this.connectivitySettings = connectivityOptions.Value;
         this.peerAddressBook = peerAddressBook;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this.RegisterStatisticFeeds();
         this.eventSubscriptionManager.RegisterSubscriptions(
            this.eventBus.Subscribe<PeerConnected>(this.AddConnectedPeer),
            this.eventBus.Subscribe<PeerDisconnected>(this.RemoveConnectedPeer)
            );

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         this.Dispose();
         return Task.CompletedTask;
      }


      public void Misbehave(IPeerContext peerContext, uint penality, string reason)
      {
         if (penality == 0)
         {
            return;
         }

         if (!this.connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            this.logger.LogWarning("Cannot attribute bad behavior to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
            // did we have to add it to banned peers anyway?
            return;
         }

         this.logger.LogDebug("Peer {PeerId} misbehave: {MisbehaveReason}.", peerContext.PeerId, reason);
         int currentResult = score.UpdateScore(-(int)penality);

         if (currentResult < connectivitySettings.BanScore)
         {
            //if threshold of bad behavior has been exceeded, this peer need to be banned
            this.logger.LogDebug("Peer {RemoteEndPoint} BAN threshold exceeded.", peerContext.RemoteEndPoint);
            this.peerAddressBook.Ban(peerContext, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(connectivitySettings.MisbehavingBanTime), "Peer Misbehaving");
         }
      }

      public void AddBonus(IPeerContext peerContext, uint bonus, string reason)
      {
         if (!this.connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            this.logger.LogWarning("Cannot attribute positive points to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
         }
         else
         {
            this.logger.LogDebug("Peer {PeerId} got a bonus {PeerBonus}: {MisbehaveReason}.", peerContext.PeerId, bonus, reason);
            score.UpdateScore((int)bonus);
         }
      }

      /// <summary>
      /// Adds the specified peer to the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void AddConnectedPeer(PeerConnected @event)
      {
         this.connectedPeers[@event.PeerContext.PeerId] = new PeerScore(@event.PeerContext, INITIAL_SCORE);
         this.logger.LogDebug("Added peer {PeerId} to the list of PeerBehaviorManager connected peers", @event.PeerContext.PeerId);
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void RemoveConnectedPeer(PeerDisconnected @event)
      {
         if (!this.connectedPeers.Remove(@event.PeerContext.PeerId))
         {
            this.logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            this.logger.LogInformation("Peer {PeerId} disconnected from PeerBehaviorManager.", @event.PeerContext.PeerId);
         }
      }

      public void Dispose()
      {
         this.eventSubscriptionManager.Dispose();
      }
   }
}
