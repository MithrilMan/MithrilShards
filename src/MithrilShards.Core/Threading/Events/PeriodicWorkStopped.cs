using System;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Threading.Events;

/// <summary>
/// Happens when a periodic work stops because of a critical exception
/// </summary>
/// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
public class PeriodicWorkCriticallyStopped : EventBase
{
   public string WorkLabel { get; }
   public Guid WorkId { get; }
   public Exception LastException { get; }
   public string? Message { get; }

   public PeriodicWorkCriticallyStopped(string WorkLabel, Guid workId, Exception lastException, string? Message)
   {
      this.WorkLabel = WorkLabel;
      WorkId = workId;
      LastException = lastException;
      this.Message = Message;
   }
}
