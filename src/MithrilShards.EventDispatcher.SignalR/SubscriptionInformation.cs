using System;
using System.Collections.Generic;

namespace MithrilShards.EventDispatcher.SignalR;

internal class SubscriptionInformation
{
   public Dictionary<string, Type> Subscriptions { get; private set; } = new();
   public string UserId { get; }

   public SubscriptionInformation(string userId)
   {
      UserId = userId;
   }

   public void Subscribe(string eventName, Type payloadType)
   {
      Subscriptions[eventName.ToLowerInvariant()] = payloadType;
   }

   public bool Unsubscribe(string eventName)
   {
      return Subscriptions.Remove(eventName.ToLowerInvariant());
   }

   public void UnsubscribeAll()
   {
      Subscriptions.Clear();
   }
}