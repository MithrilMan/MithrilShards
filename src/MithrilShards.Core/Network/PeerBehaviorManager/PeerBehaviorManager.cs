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

      private readonly Dictionary<string, PeerScore> _connectedPeers = new();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager _eventSubscriptionManager = new();

      public DefaultPeerBehaviorManager(ILogger<DefaultPeerBehaviorManager> logger,
                                        IEventBus eventBus,
                                        IStatisticFeedsCollector statisticFeedsCollector,
                                        IOptions<ForgeConnectivitySettings> connectivityOptions,
                                        IPeerAddressBook peerAddressBook)
      {
         _logger = logger;
         _eventBus = eventBus;
         _statisticFeedsCollector = statisticFeedsCollector;
         _connectivitySettings = connectivityOptions.Value;
         _peerAddressBook = peerAddressBook;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         RegisterStatisticFeeds();
         _eventSubscriptionManager.RegisterSubscriptions(
            _eventBus.Subscribe<PeerConnected>(AddConnectedPeerAsync),
            _eventBus.Subscribe<PeerDisconnected>(RemoveConnectedPeerAsync)
            );

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         Dispose();
         return Task.CompletedTask;
      }


      public void Misbehave(IPeerContext peerContext, uint penality, string reason)
      {
         if (penality == 0)
         {
            return;
         }

         if (!_connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            _logger.LogWarning("Cannot attribute bad behavior to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
            // did we have to add it to banned peers anyway?
            return;
         }

         _logger.LogDebug("Peer {PeerId} misbehave: {MisbehaveReason}.", peerContext.PeerId, reason);
         int currentResult = score.UpdateScore(-(int)penality);

         if (currentResult < _connectivitySettings.BanScore)
         {
            //if threshold of bad behavior has been exceeded, this peer need to be banned
            _logger.LogDebug("Peer {RemoteEndPoint} BAN threshold exceeded.", peerContext.RemoteEndPoint);
            _peerAddressBook.Ban(peerContext, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(_connectivitySettings.MisbehavingBanTime), "Peer Misbehaving");
         }
      }

      public void AddBonus(IPeerContext peerContext, uint bonus, string reason)
      {
         if (!_connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            _logger.LogWarning("Cannot attribute positive points to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
         }
         else
         {
            _logger.LogDebug("Peer {PeerId} got a bonus {PeerBonus}: {MisbehaveReason}.", peerContext.PeerId, bonus, reason);
            score.UpdateScore((int)bonus);
         }
      }

      public int GetScore(IPeerContext peerContext)
      {
         if (!_connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore? score))
         {
            _logger.LogWarning("Peer {PeerId} not found, returning neutral score.", peerContext.PeerId);
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
      /// <param name="event">The event.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      private ValueTask AddConnectedPeerAsync(PeerConnected @event, CancellationToken cancellationToken)
      {
         _connectedPeers[@event.PeerContext.PeerId] = new PeerScore(@event.PeerContext, INITIAL_SCORE);
         _logger.LogDebug("Added peer {PeerId} to the list of PeerBehaviorManager connected peers", @event.PeerContext.PeerId);
         return ValueTask.CompletedTask;
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="event">The event.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      private ValueTask RemoveConnectedPeerAsync(PeerDisconnected @event, CancellationToken cancellationToken)
      {
         if (!_connectedPeers.Remove(@event.PeerContext.PeerId))
         {
            _logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            _logger.LogDebug("Peer {PeerId} disconnected from PeerBehaviorManager.", @event.PeerContext.PeerId);
         }

         return ValueTask.CompletedTask;
      }

      public void Dispose()
      {
         _eventSubscriptionManager.Dispose();
      }
   }
}
