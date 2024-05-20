using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.EventBus;

public class InMemoryEventBus : IEventBus
{
   /// <summary>Instance logger.</summary>
   private readonly ILogger _logger;

   /// <summary>
   /// The subscriber error handler
   /// </summary>
   private readonly ISubscriptionErrorHandler _subscriptionErrorHandler;

   /// <summary>
   /// The subscriptions stored by EventType
   /// </summary>
   private readonly Dictionary<Type, List<ISubscription>> _subscriptions;

   /// <summary>
   /// The subscriptions lock to prevent race condition during publishing
   /// </summary>
   private readonly object _subscriptionsLock = new();

   /// <summary>
   /// Initializes a new instance of the <see cref="InMemoryEventBus" /> class.
   /// </summary>
   /// <param name="logger">The logger.</param>
   /// <param name="subscriptionErrorHandler">The subscription error handler. If null the default one will be used</param>
   public InMemoryEventBus(ILogger<InMemoryEventBus> logger, ISubscriptionErrorHandler subscriptionErrorHandler)
   {
      _logger = logger;
      _subscriptionErrorHandler = subscriptionErrorHandler;
      _subscriptions = [];
   }


   public SubscriptionToken Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler) where TEvent : EventBase
   {
      ArgumentNullException.ThrowIfNull(handler);

      lock (_subscriptionsLock)
      {
         if (!_subscriptions.TryGetValue(typeof(TEvent), out List<ISubscription>? eventSubscriptions))
         {
            eventSubscriptions = [];
            _subscriptions.Add(typeof(TEvent), eventSubscriptions);
         }

         var subscriptionToken = new SubscriptionToken(this, typeof(TEvent));
         eventSubscriptions.Add(new Subscription<TEvent>(handler, subscriptionToken));

         return subscriptionToken;
      }
   }

   /// <inheritdoc />
   public void Unsubscribe(SubscriptionToken subscriptionToken)
   {
      // Ignore null token
      if (subscriptionToken == null)
      {
         _logger.LogDebug("Unsubscribe called with a null token, ignored.");
         return;
      }

      lock (_subscriptionsLock)
      {
         if (_subscriptions.TryGetValue(subscriptionToken.EventType, out List<ISubscription>? value))
         {
            var subscriptionToRemove = value.FirstOrDefault(sub => sub.SubscriptionToken.Token == subscriptionToken.Token);
            if (subscriptionToRemove != null)
            {
               value.Remove(subscriptionToRemove);
            }
         }
      }
   }

   public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : EventBase
   {
      if (@event == null)
      {
         ThrowHelper.ThrowArgumentNullException(nameof(@event));
      }

      Type eventType = typeof(TEvent);

      List<ISubscription>? allSubscriptions = null;
      lock (_subscriptionsLock)
      {
         if (_subscriptions.TryGetValue(eventType, out List<ISubscription>? value))
         {
            allSubscriptions = [.. value];
         }
      }

      if (allSubscriptions is not null)
      {
         foreach (ISubscription? subscription in allSubscriptions)
         {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
               await subscription.ProcessEventAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
               _subscriptionErrorHandler?.Handle(@event, ex, subscription);
            }
         }
      }
   }
}
