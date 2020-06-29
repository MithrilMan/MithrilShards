using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Implementations of <see cref="IPeriodicWork"/>.
   /// Users of an <see cref="IPeriodicWork"/> should dispose it when it's not needed or call StartAsync using a cancellation token
   /// that's canceled when the periodic work is not needed anymore.
   /// Canceling a periodic work using the token is the same as calling <see cref="StopAsync"/>.
   /// Calling Dispose has the same effect as calling StopAsync.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   /// <seealso cref="MithrilShards.Core.Threading.IPeriodicWork" />
   public sealed class PeriodicWork : IDisposable, IPeriodicWork
   {
      public delegate Task WorkAsync(CancellationToken cancellation);

      private readonly ILogger logger;
      private readonly Guid id;
      private CancellationTokenSource? cancellationTokenSource;

      private volatile bool isRunning = false;
      public bool IsRunning => isRunning;

      private int exceptionsCount;
      public int ExceptionsCount => Interlocked.CompareExchange(ref exceptionsCount, 0, 0);

      private volatile Exception? lastException;
      public Exception? LastException => lastException;

      public PeriodicWork(ILogger<PeriodicWork> logger)
      {
         this.logger = logger;
         this.id = Guid.NewGuid();
      }

      public async Task StartAsync(CancellationToken cancellation, TimeSpan interval, WorkAsync work)
      {
         if (!this.isRunning)
         {
            logger.LogDebug("PeriodicWork {PeriodicWorkId} is already running.", this.id);
            return;
         }

         Interlocked.Exchange(ref exceptionsCount, 0);

         using (this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
         {
            var token = cancellationTokenSource.Token;

            logger.LogDebug("PeriodicWork {PeriodicWorkId} is starting.", this.id);
            this.isRunning = true;
            while (!token.IsCancellationRequested)
            {
               try
               {
                  await work(token).WithCancellationAsync(token).ConfigureAwait(false);
                  await Task.Delay(interval, token).ConfigureAwait(false);
               }
               catch (OperationCanceledException)
               {
                  logger.LogDebug("PeriodicWork {PeriodicWorkId} aborted.", this.id);
                  break;
               }
               catch (Exception ex)
               {
                  lastException = ex;
                  Interlocked.Increment(ref exceptionsCount);
               }
            }

            this.isRunning = false;

            if (this.ExceptionsCount == 0)
            {
               logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated without errors.", this.id);
            }
            else
            {
               logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated with {PeriodicWorkErrors} errors.", this.id, this.ExceptionsCount);
            }
         }
      }

      public Task StopAsync()
      {
         if (!this.isRunning)
         {
            logger.LogDebug("PeriodicWork {PeriodicWorkId} is not running.", this.id);
            return Task.CompletedTask;
         }

         if (!this.cancellationTokenSource?.IsCancellationRequested ?? true)
         {
            logger.LogDebug("PeriodicWork {PeriodicWorkId} is stopping.", this.id);
            this.cancellationTokenSource?.Cancel();
            this.cancellationTokenSource?.Dispose();
            this.cancellationTokenSource = null;
         }

         return Task.CompletedTask;
      }


      public void Dispose()
      {
         this.StopAsync();

         GC.SuppressFinalize(this);
      }
   }
}
