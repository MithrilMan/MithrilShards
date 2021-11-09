using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.EventBus;

/// <summary>
/// Event Bus interface
/// </summary>
public interface IEventBus
{
   /// <summary>
   /// Subscribes to the specified event type with the specified asynchronous action.
   /// </summary>
   /// <typeparam name="TEvent">The type of event.</typeparam>
   /// <param name="handler">The handler to invoke when an event of this type is published.</param>
   /// <returns>A <see cref="SubscriptionToken"/> to be used when calling <see cref="Unsubscribe"/>.</returns>
   SubscriptionToken Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler) where TEvent : EventBase;

   /// <summary>
   /// Unsubscribe from the Event type related to the specified <see cref="SubscriptionToken"/>.
   /// </summary>
   /// <param name="token">The <see cref="SubscriptionToken"/> received from calling the Subscribe method.</param>
   void Unsubscribe(SubscriptionToken token);

   /// <summary>
   /// Publishes the specified event to any subscribers for the <typeparamref name="TEvent" /> event type.
   /// </summary>
   /// <typeparam name="TEvent">The type of event.</typeparam>
   /// <param name="eventItem">Event to publish.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   Task PublishAsync<TEvent>(TEvent eventItem, CancellationToken cancellationToken = default) where TEvent : EventBase;
}
