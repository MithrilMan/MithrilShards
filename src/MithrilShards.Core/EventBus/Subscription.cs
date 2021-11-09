using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.EventBus;

internal class Subscription<TEventBase> : ISubscription where TEventBase : EventBase
{
   /// <summary>
   /// Token returned to the subscriber
   /// </summary>
   public SubscriptionToken SubscriptionToken { get; }

   /// <summary>
   /// The action to invoke when a subscripted event type is published.
   /// </summary>
   private readonly Func<TEventBase, CancellationToken, ValueTask> _action;

   public Subscription(Func<TEventBase, CancellationToken, ValueTask> action, SubscriptionToken token)
   {
      _action = action ?? throw new ArgumentNullException(nameof(action));
      SubscriptionToken = token ?? throw new ArgumentNullException(nameof(token));
   }

   public ValueTask ProcessEventAsync(EventBase eventItem, CancellationToken cancellationToken)
   {
      if (eventItem is null) throw new ArgumentNullException(nameof(eventItem));

      if (!(eventItem is TEventBase))
      {
         throw new ArgumentException("Event Item is not the correct type.");
      }

      if (cancellationToken.IsCancellationRequested)
      {
         return ValueTask.CompletedTask;
      }

      return _action.Invoke((TEventBase)eventItem, cancellationToken);
   }
}
