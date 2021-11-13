using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.EventDispatcher.SignalR;

public interface IEventSubscriptionManager
{
   Task StopAsync(CancellationToken cancellationToken);

   void RegisterConnection(string connectionId);

   void UnregisterConnection(string connectionId);

   /// <summary>
   /// Subscribes to a specified event identified by <paramref name="eventName"/> (case insensitive).
   /// Whenever an event of that kind is generated the client will get back a <typeparamref name="TEventPayload"/> containing
   /// properties that matches between the original event and the required Type.
   /// </summary>
   /// <typeparam name="TEventPayload"></typeparam>
   /// <param name="connectionId"></param>
   /// <param name="eventName">The case insensitive event name.</param>
   /// <returns>An instance of <typeparamref name="TEventPayload"/> containing data that was successfully mapped</returns>
   Task<TEventPayload> SubscribeToAsync<TEventPayload>(string connectionId, string eventName);

   /// <summary>
   /// Unsubscribes from the specified <paramref name="eventName"/> (case insensitive).
   /// </summary>
   /// <param name="connectionId"></param>
   /// <param name="eventName">The case insensitive event name.</param>
   /// <returns>true if the subscription has been removed, false if there was no subscription for the specified event name.</returns>
   Task<bool> UnsubscribeFromAsync(string connectionId, string eventName);
}
