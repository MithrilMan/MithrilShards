using System.Threading.Tasks;

namespace MithrilShards.EventDispatcher.SignalR.Hubs;

/// <summary>
/// Defines SignalR Hub server methods that can be invoked from the client.
/// </summary>
public interface IEventDispatcherServer
{
   /// <summary>
   /// Subscribes to a specified event identified by <paramref name="eventName"/> (case insensitive).
   /// Whenever an event of that kind is generated the client will get back a <typeparamref name="TEventPayload"/> containing
   /// properties that matches between the original event and the required Type.
   /// </summary>
   /// <typeparam name="TEventPayload"></typeparam>
   /// <param name="eventName">The case insensitive event name.</param>
   /// <returns>An instance of <typeparamref name="TEventPayload"/> containing data that was successfully mapped</returns>
   Task<TEventPayload> SubscribeToAsync<TEventPayload>(string eventName);

   /// <summary>
   /// Unsubscribes from the specified <paramref name="eventName"/> (case insensitive).
   /// </summary>
   /// <param name="eventName">The case insensitive event name.</param>
   /// <returns>true if the subscription has been removed, false if there was no subscription for the specified event name.</returns>
   Task<bool> UnsubscribeFromAsync(string eventName);
}
