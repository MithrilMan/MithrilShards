using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Threading
{
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
      /// Starts the asynchronous periodic work.
      /// </summary>
      /// <param name="cancellation">The cancellation token.</param>
      /// <param name="interval">The interval of time that will be awaited before executing again the work.</param>
      /// <param name="work">The work to execute.</param>
      Task StartAsync(CancellationToken cancellation, TimeSpan interval, PeriodicWork.WorkAsync work);

      /// <summary>
      /// Stops the asynchronous periodic work.
      /// </summary>
      Task StopAsync();
   }
}