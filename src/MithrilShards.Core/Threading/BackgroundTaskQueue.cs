using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Threading;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
   private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
   private readonly SemaphoreSlim _signal = new(0);

   public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
   {
      ArgumentNullException.ThrowIfNull(workItem);

      _workItems.Enqueue(workItem);
      _signal.Release();
   }

   public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
   {
      await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
      _workItems.TryDequeue(out Func<CancellationToken, Task>? workItem);

      return workItem;
   }
}
