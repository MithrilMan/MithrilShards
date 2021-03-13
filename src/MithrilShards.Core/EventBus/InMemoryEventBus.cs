using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.EventBus
{
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
      private readonly object _subscriptionsLock = new object();

      /// <summary>
      /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
      /// </summary>
      /// <param name="loggerFactory">The logger factory.</param>
      /// <param name="subscriptionErrorHandler">The subscription error handler. If null the default one will be used</param>
      public InMemoryEventBus(ILogger<InMemoryEventBus> logger, ISubscriptionErrorHandler subscriptionErrorHandler)
      {
         _logger = logger;
         _subscriptionErrorHandler = subscriptionErrorHandler;
         _subscriptions = new Dictionary<Type, List<ISubscription>>();
      }

      /// <inheritdoc />
      public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventBase
      {
         if (handler == null)
         {
            throw new ArgumentNullException(nameof(handler));
         }

         lock (_subscriptionsLock)
         {
            if (!_subscriptions.ContainsKey(typeof(TEvent)))
            {
               _subscriptions.Add(typeof(TEvent), new List<ISubscription>());
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var subscriptionToken = new SubscriptionToken(this, typeof(TEvent));
#pragma warning restore CA2000 // Dispose objects before losing scope

            _subscriptions[typeof(TEvent)].Add(new Subscription<TEvent>(handler, subscriptionToken));

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
            if (_subscriptions.ContainsKey(subscriptionToken.EventType))
            {
               List<ISubscription> allSubscriptions = _subscriptions[subscriptionToken.EventType];

               ISubscription? subscriptionToRemove = allSubscriptions.FirstOrDefault(sub => sub.SubscriptionToken.Token == subscriptionToken.Token);
               if (subscriptionToRemove != null)
               {
                  _subscriptions[subscriptionToken.EventType].Remove(subscriptionToRemove);
               }
            }
         }
      }

      /// <inheritdoc />
      public void Publish<TEvent>(TEvent @event) where TEvent : EventBase
      {
         if (@event == null)
         {
            throw new ArgumentNullException(nameof(@event));
         }

         var allSubscriptions = new List<ISubscription>();
         lock (_subscriptionsLock)
         {
            Type eventType = typeof(TEvent);
            if (_subscriptions.ContainsKey(eventType))
            {
               allSubscriptions = _subscriptions[eventType].ToList();
            }
         }

         for (int index = 0; index < allSubscriptions.Count; index++)
         {
            ISubscription subscription = allSubscriptions[index];
            try
            {
               subscription.Publish(@event);
            }
            catch (Exception ex)
            {
               _subscriptionErrorHandler?.Handle(@event, ex, subscription);
            }
         }
      }
   }
}
