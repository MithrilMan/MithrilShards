using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Extensions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "<Pending>")]
public static class TaskExtensions
{
   /// <summary>
   /// Wraps a task with one that will complete as cancelled based on a cancellation token,
   /// allowing someone to await a task but be able to break out early by cancelling the token.
   /// </summary>
   /// <typeparam name="T">The type of value returned by the task.</typeparam>
   /// <param name="task">The task to wrap.</param>
   /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
   /// <returns>The wrapping task.</returns>
   public static Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken cancellationToken)
   {
      if (task is null) throw new ArgumentNullException(nameof(task));

      if (!cancellationToken.CanBeCanceled || task.IsCompleted)
      {
         return task;
      }

      if (cancellationToken.IsCancellationRequested)
      {
         return Task.FromCanceled<T>(cancellationToken);
      }

      return WithCancellationSlow(task, cancellationToken);
   }


   /// <summary>
   /// Wraps a task with one that will complete as cancelled based on a cancellation token,
   /// allowing someone to await a task but be able to break out early by cancelling the token.
   /// </summary>
   /// <param name="task">The task to wrap.</param>
   /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
   /// <returns>The wrapping task.</returns>
   public static Task WithCancellationAsync(this Task task, CancellationToken cancellationToken)
   {
      if (task is null) ThrowHelper.ThrowArgumentNullException(nameof(task));

      if (!cancellationToken.CanBeCanceled || task.IsCompleted)
      {
         return task;
      }

      if (cancellationToken.IsCancellationRequested)
      {
         return Task.FromCanceled(cancellationToken);
      }

      return WithCancellationSlow(task, continueOnCapturedContext: false, cancellationToken: cancellationToken);
   }

   /// <summary>
   /// Wraps a task with one that will complete as cancelled based on a cancellation token,
   /// allowing someone to await a task but be able to break out early by cancelling the token.
   /// </summary>
   /// <typeparam name="T">The type of value returned by the task.</typeparam>
   /// <param name="task">The task to wrap.</param>
   /// <param name="continueOnCapturedContext">A value indicating whether *internal* continuations required to respond to cancellation should run on the current <see cref="SynchronizationContext"/>.</param>
   /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
   /// <returns>The wrapping task.</returns>
   private static async Task<T> WithCancellationSlow<T>(Task<T> task, CancellationToken cancellationToken, bool continueOnCapturedContext = false)
   {
      var tcs = new TaskCompletionSource<bool>();
      using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
      {
         if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(continueOnCapturedContext))
         {
            cancellationToken.ThrowIfCancellationRequested();
         }
      }

      // Rethrow any fault/cancellation exception, even if we awaited above.
      // But if we skipped the above if branch, this will actually yield
      // on an incompleted task.
      return await task.ConfigureAwait(continueOnCapturedContext);
   }

   /// <summary>
   /// Wraps a task with one that will complete as cancelled based on a cancellation token,
   /// allowing someone to await a task but be able to break out early by cancelling the token.
   /// </summary>
   /// <param name="task">The task to wrap.</param>
   /// <param name="continueOnCapturedContext">A value indicating whether *internal* continuations required to respond to cancellation should run on the current <see cref="SynchronizationContext"/>.</param>
   /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
   /// <returns>The wrapping task.</returns>
   private static async Task WithCancellationSlow(this Task task, CancellationToken cancellationToken, bool continueOnCapturedContext = false)
   {
      var tcs = new TaskCompletionSource<bool>();
      using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
      {
         if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(continueOnCapturedContext))
         {
            cancellationToken.ThrowIfCancellationRequested();
         }
      }

      await task.ConfigureAwait(continueOnCapturedContext);
   }
}
