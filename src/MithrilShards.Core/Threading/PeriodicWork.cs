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
   /// Implementations of <see cref="IPeriodicWork" />.
   /// Users of an <see cref="IPeriodicWork" /> should dispose it when it's not needed or call StartAsync using a cancellation token
   /// that's canceled when the periodic work is not needed anymore.
   /// Canceling a periodic work using the token is the same as calling <see cref="StopAsync" />.
   /// Calling Dispose has the same effect as calling StopAsync.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Threading.IPeriodicWork" />
   /// <seealso cref="System.IDisposable" />
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
         _logger = logger;
         _periodicWorkTracker = periodicWorkTracker;
         _eventBus = eventBus;

         Id = Guid.NewGuid();
         logger.LogDebug("Created periodic work {IPeriodicWorkId}", Id);
      }

      public void Configure(bool stopOnException = false, IPeriodicWorkExceptionHandler? exceptionHandler = null)
      {
         _stopOnException = stopOnException;
         _exceptionHandler = exceptionHandler;
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
         if (_isRunning)
         {
            _logger.LogError("PeriodicWork {PeriodicWorkId} is already running.", Id);
            ThrowHelper.ThrowInvalidOperationException("Cannot start a periodic work that's already running.");
            return;
         }

         _label = label;
         Interlocked.Exchange(ref _exceptionsCount, 0);

         _periodicWorkTracker.StartTracking(this);

         using (_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
         {
            CancellationToken token = _cancellationTokenSource.Token;

            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is starting.", Id);
            _isRunning = true;
            while (!token.IsCancellationRequested)
            {
               try
               {
                  await work(token).WithCancellationAsync(token).ConfigureAwait(false);
                  await Task.Delay(interval()).WithCancellationAsync(token).ConfigureAwait(false);
               }
               catch (OperationCanceledException)
               {
                  _logger.LogDebug("PeriodicWork {PeriodicWorkId} aborted.", Id);
                  break;
               }
               catch (Exception ex)
               {
                  _lastException = ex;
                  Interlocked.Increment(ref _exceptionsCount);

                  var feedback = new IPeriodicWorkExceptionHandler.Feedback(!_stopOnException, false, null);
                  _exceptionHandler?.OnPeriodicWorkException(this, ex, ref feedback);

                  if (!feedback.ContinueExecution)
                  {
                     _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated because of {PeriodicWorkException}.", Id, ex);
                     _isRunning = false;

                     if (feedback.IsCritical)
                     {
                        await _eventBus.PublishAsync(new PeriodicWorkCriticallyStopped(_label, Id, _lastException, feedback.Message), cancellation).ConfigureAwait(false);
                     }

                     return;
                  }
               }
            }

            _isRunning = false;

            if (ExceptionsCount == 0)
            {
               _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated without errors.", Id);
            }
            else
            {
               _logger.LogDebug("PeriodicWork {PeriodicWorkId} terminated with {PeriodicWorkErrors} errors.", Id, ExceptionsCount);
            }
         }

         _periodicWorkTracker.StopTracking(this);
      }

      public Task StopAsync()
      {
         if (!_isRunning)
         {
            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is not running.", Id);
            return Task.CompletedTask;
         }

         if (!_cancellationTokenSource?.IsCancellationRequested ?? true)
         {
            _logger.LogDebug("PeriodicWork {PeriodicWorkId} is stopping.", Id);
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
         }

         return Task.CompletedTask;
      }


      public void Dispose()
      {
         if (_isRunning)
         {
            _ = StopAsync();
         }

         GC.SuppressFinalize(this);
      }
   }
}
