using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MithrilShards.EventDispatcher.SignalR.Hubs;

public class EventDispatcherHub : Hub<IEventDispatcherClient>, IEventDispatcherServer
{
   readonly ILogger<EventDispatcherHub> _logger;
   readonly IEventSubscriptionManager _eventSubscriptionManager;

   public EventDispatcherHub(ILogger<EventDispatcherHub> logger, IEventSubscriptionManager eventSubscriptionManager)
   {
      _logger = logger;
      _eventSubscriptionManager = eventSubscriptionManager;
   }

   public static string GetGroupName(string domain, string idFleet) => $"{domain}-{idFleet}";

   public override async Task OnConnectedAsync()
   {
      _eventSubscriptionManager.RegisterConnection(Context.ConnectionId);

      await base.OnConnectedAsync().ConfigureAwait(false);
   }

   public override async Task OnDisconnectedAsync(Exception? exception)
   {
      /// when a connection ends, we remove event subscriptions
      _eventSubscriptionManager.UnregisterConnection(Context.ConnectionId);

      await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
   }

   public Task<TEventPayload> SubscribeToAsync<TEventPayload>(string eventName)
   {
      return _eventSubscriptionManager.SubscribeToAsync<TEventPayload>(Context.ConnectionId, eventName);
   }

   public Task<bool> UnsubscribeFromAsync(string eventName)
   {
      return _eventSubscriptionManager.UnsubscribeFromAsync(Context.ConnectionId, eventName);
   }
}
