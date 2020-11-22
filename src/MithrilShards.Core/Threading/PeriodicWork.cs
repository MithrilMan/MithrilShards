using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Threading.Events;

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
      private readonly ILogger _logger;
      readonly IPeriodicWorkTracker _periodicWorkTracker;
      readonly IEventBus _eventBus;
      private CancellationTokenSource? _cancellationTokenSource;

      public Guid Id { get; }

      private volatile bool _isRunning = false;
      public bool IsRunning => _isRunning;

      private int _exceptionsCount;
      public int ExceptionsCount => Interlocked.CompareExchange(ref _exceptionsCount, 0, 0);

      private volatile Exception? _lastException;
      public Exception? LastException => _lastException;

      private volatile string _label = string.Empty;
      public string Label => _label;

      private bool _stopOnException;
      public bool StopOnException => _stopOnException;

      private IPeriodicWorkExceptionHandler? _exceptionHandler = null;

      public PeriodicWork(ILogger<PeriodicWork> logger, IPeriodicWorkTracker periodicWorkTracker, IEventBus eventBus)
      {
         this._logger = logger;
         this._periodicWorkTracker = periodicWorkTracker;
         this._eventBus = eventBus;

         this.Id = Guid.NewGuid();
         logger.LogDebug("Created periodic work {IPeriodicWorkId}", this.Id);
      }

      public void Configure(bool stopOnException = false, IPeriodicWorkExceptionHandler? exceptionHandler = null)
      {
         this._stopOnException = stopOnException;
         this._exceptionHandler = exceptionHandler;
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
         if (this._isRunning)
         {
            _logger.LogError("PeriodicWork {PeriodicWorkId} is already running.", this.Id);
            ThrowHelper.ThrowInvalidOperationException("Cannot start a periodic work that's already running.");
            return;
         }

         this._label = label;
         Interlocked.Exchange(ref _exceptionsCount, 0);

         this._periodicWorkTracker.StartTracking(this);

         using (this._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
         {
            CancellationToken token = _cancellationTokenSource.Token;

            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is starting.", this.Id);
            this._isRunning = true;
            while (!token.IsCancellationRequested)
            {
               try
               {
                  await work(token).WithCancellationAsync(token).ConfigureAwait(false);
                  await Task.Delay(interval()).WithCancellationAsync(token).ConfigureAwait(false);
               }
               catch (OperationCanceledException)
               {
                  _logger.LogDebug("PeriodicWork {PeriodicWorkId} aborted.", this.Id);
                  break;
               }
               catch (Exception ex)
               {
                  _lastException = ex;
                  Interlocked.Increment(ref _exceptionsCount);

                  var feedback = new IPeriodicWorkExceptionHandler.Feedback(!_stopOnException, false, null);
                  this._exceptionHandler?.OnPeriodicWorkException(this, ex, ref feedback);

                  if (!feedback.ContinueExecution)
                  {
                     _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated because of {PeriodicWorkException}.", this.Id, ex);
                     this._isRunning = false;

                     if (feedback.IsCritical)
                     {
                        this._eventBus.Publish(new PeriodicWorkCriticallyStopped(this._label, this.Id, this._lastException, feedback.Message));
                     }

                     return;
                  }
               }
            }

            this._isRunning = false;

            if (this.ExceptionsCount == 0)
            {
               _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated without errors.", this.Id);
            }
            else
            {
               _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated with {PeriodicWorkErrors} errors.", this.Id, this.ExceptionsCount);
            }
         }

         this._periodicWorkTracker.StopTracking(this);
      }

      public Task StopAsync()
      {
         if (!this._isRunning)
         {
            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is not running.", this.Id);
            return Task.CompletedTask;
         }

         if (!this._cancellationTokenSource?.IsCancellationRequested ?? true)
         {
            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is stopping.", this.Id);
            this._cancellationTokenSource?.Cancel();
            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = null;
         }

         return Task.CompletedTask;
      }


      public void Dispose()
      {
         if (this._isRunning)
         {
            this.StopAsync();
         }

         GC.SuppressFinalize(this);
      }
   }
}
