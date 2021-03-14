using System;

namespace MithrilShards.Core.EventBus
{
   /// <summary>
   /// Basic abstract event implementation.
   /// </summary>
   public abstract class EventBase
   {
      /// <inheritdoc />
      public Guid CorrelationId { get; }

      public EventBase()
      {
         // Assigns an unique id to the event.
         CorrelationId = Guid.NewGuid();
      }

      public override string ToString()
      {
         return $"{CorrelationId.ToString()} - {GetType().Name}";
      }
   }
}
