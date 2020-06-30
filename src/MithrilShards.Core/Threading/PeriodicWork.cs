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
      private readonly ILogger logger;
      readonly IPeriodicWorkTracker periodicWorkTracker;
      private CancellationTokenSource? cancellationTokenSource;

      public Guid Id { get; }

      private volatile bool isRunning = false;
      public bool IsRunning => isRunning;

      private int exceptionsCount;
      public int ExceptionsCount => Interlocked.CompareExchange(ref exceptionsCount, 0, 0);

      private volatile Exception? lastException;
      public Exception? LastException => lastException;

      private volatile string label = string.Empty;
      public string Label => label;

      private bool stopOnException;
      public bool StopOnException => stopOnException;

      private IPeriodicWorkExceptionHandler? exceptionHandler = null;

      public PeriodicWork(ILogger<PeriodicWork> logger, IPeriodicWorkTracker periodicWorkTracker)
      {
         this.logger = logger;
         this.periodicWorkTracker = periodicWorkTracker;

         this.Id = Guid.NewGuid();
      }

      public void Configure(bool stopOnException = false, IPeriodicWorkExceptionHandler? exceptionHandler = null)
      {
         this.stopOnException = stopOnException;
         this.exceptionHandler = exceptionHandler;
      }

      public Task StartAsync(string label, IPeriodicWork.WorkAsync work, TimeSpan interval, CancellationToken cancellation)
      {
         return StartInternalAsync(label, work, () => interval, cancellation);
      }

      public Task StartAsync(string label, IPeriodicWork.WorkAsync work, Func<TimeSpan> interval, CancellationToken cancellation)
      {
         return StartInternalAsync(label, work, interval, cancellation);
      }

      private async Task StartInternalAsync(string label, IPeriodicWork.WorkAsync work, Func<TimeSpan> interval, CancellationToken cancellation)
      {
         if (this.isRunning)
         {
            logger.LogError("PeriodicWork {PeriodicWorkId} is already running.", this.Id);
            ThrowHelper.ThrowInvalidOperationException("Cannot start a periodic work that's already running.");
            return;
         }

         this.label = label;
         Interlocked.Exchange(ref exceptionsCount, 0);

         this.periodicWorkTracker.StartTracking(this);

         using (this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
         {
            var token = cancellationTokenSource.Token;

            logger.LogDebug("PeriodicWork {PeriodicWorkId} is starting.", this.Id);
            this.isRunning = true;
            while (!token.IsCancellationRequested)
            {
               try
               {
                  await work(token).WithCancellationAsync(token).ConfigureAwait(false);
                  await Task.Delay(interval()).WithCancellationAsync(token).ConfigureAwait(false);
               }
               catch (OperationCanceledException)
               {
                  logger.LogDebug("PeriodicWork {PeriodicWorkId} aborted.", this.Id);
                  break;
               }
               catch (Exception ex)
               {
                  lastException = ex;
                  Interlocked.Increment(ref exceptionsCount);

                  bool continueExecution = false;
                  this.exceptionHandler?.OnException(this, ex, out continueExecution);

                  if (stopOnException || !continueExecution)
                  {
                     logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated because of {PeriodicWorkException}.", this.Id, ex);
                     this.isRunning = false;
                     return;
                  }
               }
            }

            this.isRunning = false;

            if (this.ExceptionsCount == 0)
            {
               logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated without errors.", this.Id);
            }
            else
            {
               logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated with {PeriodicWorkErrors} errors.", this.Id, this.ExceptionsCount);
            }
         }

         this.periodicWorkTracker.StopTracking(this);
      }

      public Task StopAsync()
      {
         if (!this.isRunning)
         {
            logger.LogDebug("PeriodicWork {PeriodicWorkId} is not running.", this.Id);
            return Task.CompletedTask;
         }

         if (!this.cancellationTokenSource?.IsCancellationRequested ?? true)
         {
            logger.LogDebug("PeriodicWork {PeriodicWorkId} is stopping.", this.Id);
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
