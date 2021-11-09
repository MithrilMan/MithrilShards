using System;

namespace MithrilShards.Core.EventBus;

public interface ISubscriptionErrorHandler
{
   /// <summary>
   /// Handles the specified event error.
   /// </summary>
   /// <param name="theEvent">The event that caused the error.</param>
   /// <param name="exception">The exception raised.</param>
   /// <param name="subscription">The subscription that generated the error.</param>
   void Handle(EventBase theEvent, Exception exception, ISubscription subscription);
}
