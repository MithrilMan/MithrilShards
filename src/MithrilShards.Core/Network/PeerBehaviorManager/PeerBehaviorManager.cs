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
      private readonly ILogger<DefaultPeerBehaviorManager> _logger;
      private readonly IEventBus _eventBus;
      private readonly ForgeConnectivitySettings _connectivitySettings;
      private readonly IPeerAddressBook _peerAddressBook;

      private readonly Dictionary<string, PeerScore> _connectedPeers = new Dictionary<string, PeerScore>();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager _eventSubscriptionManager = new EventSubscriptionManager();

      public DefaultPeerBehaviorManager(ILogger<DefaultPeerBehaviorManager> logger,
                                        IEventBus eventBus,
                                        IStatisticFeedsCollector statisticFeedsCollector,
                                        IOptions<ForgeConnectivitySettings> connectivityOptions,
                                        IPeerAddressBook peerAddressBook)
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._statisticFeedsCollector = statisticFeedsCollector;
         this._connectivitySettings = connectivityOptions.Value;
         this._peerAddressBook = peerAddressBook;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this.RegisterStatisticFeeds();
         this._eventSubscriptionManager.RegisterSubscriptions(
            this._eventBus.Subscribe<PeerConnected>(this.AddConnectedPeer),
            this._eventBus.Subscribe<PeerDisconnected>(this.RemoveConnectedPeer)
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

         if (!this._connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            this._logger.LogWarning("Cannot attribute bad behavior to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
            // did we have to add it to banned peers anyway?
            return;
         }

         this._logger.LogDebug("Peer {PeerId} misbehave: {MisbehaveReason}.", peerContext.PeerId, reason);
         int currentResult = score.UpdateScore(-(int)penality);

         if (currentResult < _connectivitySettings.BanScore)
         {
            //if threshold of bad behavior has been exceeded, this peer need to be banned
            this._logger.LogDebug("Peer {RemoteEndPoint} BAN threshold exceeded.", peerContext.RemoteEndPoint);
            this._peerAddressBook.Ban(peerContext, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(_connectivitySettings.MisbehavingBanTime), "Peer Misbehaving");
         }
      }

      public void AddBonus(IPeerContext peerContext, uint bonus, string reason)
      {
         if (!this._connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            this._logger.LogWarning("Cannot attribute positive points to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
         }
         else
         {
            this._logger.LogDebug("Peer {PeerId} got a bonus {PeerBonus}: {MisbehaveReason}.", peerContext.PeerId, bonus, reason);
            score.UpdateScore((int)bonus);
         }
      }

      public int GetScore(IPeerContext peerContext)
      {
         if (!this._connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            this._logger.LogWarning("Peer {PeerId} not found, returning neutral score.", peerContext.PeerId);
            return 0;
         }
         else
         {
            return score.Score;
         }
      }

      /// <summary>
      /// Adds the specified peer to the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void AddConnectedPeer(PeerConnected @event)
      {
         this._connectedPeers[@event.PeerContext.PeerId] = new PeerScore(@event.PeerContext, INITIAL_SCORE);
         this._logger.LogDebug("Added peer {PeerId} to the list of PeerBehaviorManager connected peers", @event.PeerContext.PeerId);
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void RemoveConnectedPeer(PeerDisconnected @event)
      {
         if (!this._connectedPeers.Remove(@event.PeerContext.PeerId))
         {
            this._logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            this._logger.LogInformation("Peer {PeerId} disconnected from PeerBehaviorManager.", @event.PeerContext.PeerId);
         }
      }

      public void Dispose()
      {
         this._eventSubscriptionManager.Dispose();
      }
   }
}
