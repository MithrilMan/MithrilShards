using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.EventDispatcher.SignalR.Hubs;

namespace MithrilShards.EventDispatcher.SignalR;

public class EventSubscriptionManager : IEventSubscriptionManager, IHostedService
{
   private readonly ILogger<EventSubscriptionManager> _logger;
   readonly IHubContext<EventDispatcherHub, IEventDispatcherClient> _hubContext;
   readonly IEventBus _eventBus;
   readonly Dictionary<string, SubscriptionInformation> _userSubscriptions = new();
   private readonly object _lock = new();


   public EventSubscriptionManager(ILogger<EventSubscriptionManager> logger, IHubContext<EventDispatcherHub, IEventDispatcherClient> hubContext, IEventBus eventBus)
   {
      _logger = logger;
      _hubContext = hubContext;
      _eventBus = eventBus;
   }


   public void RegisterConnection(string connectionId)
   {
      lock (_lock)
      {
         if (!_userSubscriptions.TryGetValue(connectionId, out SubscriptionInformation? sub))
         {
            sub = new SubscriptionInformation(connectionId);
         }

         _eventBus.Subscribe<EventBase>(OnEventPublished);
      }
   }

   private ValueTask OnEventPublished(EventBase @event, CancellationToken cancellationToken)
   {
      throw new NotImplementedException();
   }

   public void UnregisterConnection(string connectionId)
   {
      lock (_lock)
      {
         if (!_userSubscriptions.TryGetValue(connectionId, out SubscriptionInformation? sub))
         {
            sub = new SubscriptionInformation(connectionId);
            _userSubscriptions[connectionId] = sub;
         }

         sub.UnsubscribeAll();
      }
   }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }

   public Task StopAsync(CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }

   public Task<TEventPayload> SubscribeToAsync<TEventPayload>(string connectionId, string eventName)
   {
      throw new NotImplementedException();
   }

   public Task<bool> UnsubscribeFromAsync(string connectionId, string eventName)
   {
      throw new NotImplementedException();
   }
}
