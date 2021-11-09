using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Threading;

/// <summary>
/// Execute a work with a specified interval between each work execution.
/// </summary>
/// <remarks>
/// Users of an <see cref="IPeriodicWork"/> should dispose it when it's not needed.
/// A good implementation would cancel the work using the specified issued cancellation token.
/// </remarks>
/// <seealso cref="System.IDisposable" />
public interface IPeriodicWork : IDisposable
{
   public delegate Task WorkAsync(CancellationToken cancellation);

   /// <summary>
   /// Gets the periodic work instance identifier.
   /// </summary>
   public Guid Id { get; }

   public string Label { get; }

   /// <summary>
   /// Gets a value indicating whether this periodic work is running.
   /// </summary>
   bool IsRunning { get; }

   /// <summary>
   /// Gets the exceptions count happened during work.
   /// </summary>
   int ExceptionsCount { get; }

   /// <summary>
   /// Gets the latest exception happened during work.
   /// </summary>
   Exception? LastException { get; }

   /// <summary>
   /// Gets a value indicating whether the worker stops if an exception happens (except when the task is canceled).
   /// </summary>
   bool StopOnException { get; }

   /// <summary>
   /// Configures the specified instance.
   /// </summary>
   /// <param name="stopOnException">
   /// If set to <c>true</c> stops the execution when an exception happens (except when the task is canceled).
   /// This may be overridden handling <paramref name="exceptionHandler"/>.
   /// </param>
   /// <param name="exceptionHandler ">
   /// Exception handler invoked whenever an unhandled exception happens during work execution.
   /// Handling the exception allows to override the <paramref name="stopOnException"/> flag acting on <see cref="IPeriodicWorkExceptionHandler.Feedback"/>
   /// </param>
   void Configure(bool stopOnException = false, IPeriodicWorkExceptionHandler? exceptionHandler = null);

   /// <summary>
   /// Starts the asynchronous periodic work.
   /// </summary>
   /// <param name="label"></param>
   /// <param name="cancellation">The cancellation token.</param>
   /// <param name="interval">
   /// The interval of time that will be awaited before executing again the work.
   /// Specifying TimeSpan.Zero acts as a continuous loop.
   /// </param>
   /// <param name="work">The work to execute.</param>
   Task StartAsync(string label, WorkAsync work, TimeSpan interval, CancellationToken cancellation);

   /// <summary>
   /// Starts the asynchronous periodic work with a dynamic interval between iterations.
   /// </summary>
   /// <param name="label"></param>
   /// <param name="cancellation">The cancellation token.</param>
   /// <param name="interval">A function that return the interval of time to wait before next execution.</param>
   /// <param name="work">The work to execute.</param>
   /// <returns></returns>
   Task StartAsync(string label, WorkAsync work, Func<TimeSpan> interval, CancellationToken cancellation);

   /// <summary>
   /// Stops the asynchronous periodic work.
   /// </summary>
   Task StopAsync();
}
