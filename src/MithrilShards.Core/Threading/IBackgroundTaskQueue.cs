using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Threading;

public interface IBackgroundTaskQueue
{
   void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

   Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken);
}
