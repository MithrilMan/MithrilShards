using System;

namespace MithrilShards.Core.EventBus
{
   /// <summary>
   /// Represents a subscription token.
   /// </summary>
   public class SubscriptionToken : IDisposable
   {
      public IEventBus Bus { get; }

      public Guid Token { get; }

      public Type EventType { get; }

      internal SubscriptionToken(IEventBus bus, Type eventType)
      {
         Bus = bus;
         Token = Guid.NewGuid();
         EventType = eventType;
      }

      public void Dispose()
      {
         Bus.Unsubscribe(this);
      }
   }
}
