using System;

namespace MithrilShards.Core.EventBus
{
   internal class Subscription<TEventBase> : ISubscription where TEventBase : EventBase
   {
      /// <summary>
      /// Token returned to the subscriber
      /// </summary>
      public SubscriptionToken SubscriptionToken { get; }

      /// <summary>
      /// The action to invoke when a subscripted event type is published.
      /// </summary>
      private readonly Action<TEventBase> _action;

      public Subscription(Action<TEventBase> action, SubscriptionToken token)
      {
         _action = action ?? throw new ArgumentNullException(nameof(action));
         SubscriptionToken = token ?? throw new ArgumentNullException(nameof(token));
      }

      public void Publish(EventBase eventItem)
      {
         if (eventItem is null) throw new ArgumentNullException(nameof(eventItem));

         if (!(eventItem is TEventBase))
         {
            throw new ArgumentException("Event Item is not the correct type.");
         }

         _action.Invoke((TEventBase)eventItem);
      }
   }
}
