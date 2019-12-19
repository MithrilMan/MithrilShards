using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Extensions {
   public static class TaskExtensions {
      //   /// <summary>
      //   /// Throws OperationCanceledException if token cancels before the real task completes.
      //   /// Doesn't abort the inner task, but allows the calling code to get "unblocked" and react to stuck tasks.
      //   /// </summary>
      //   public static Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken token, bool useSynchronizationContext = false, bool continueOnCapturedContext = false) {
      //      if (!token.CanBeCanceled || task.IsCompleted) {
      //         return task;
      //      }
      //      else if (token.IsCancellationRequested) {
      //         return Task.FromCanceled<T>(token);
      //      }

      //      return Inner(task, token, useSynchronizationContext, continueOnCapturedContext);

      //      static async Task<T> Inner(Task<T> task, CancellationToken token, bool useSynchronizationContext, bool continueOnCapturedContext) {
      //         var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      //         using (token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs, useSynchronizationContext)) {
      //            if (task != await Task.WhenAny(task, tcs.Task)) {
      //               token.ThrowIfCancellationRequested();
      //            }
      //         }

      //         // This is needed to re-throw eventual task exception that would be grouped instead.
      //         return await task.ConfigureAwait(continueOnCapturedContext);
      //      }
      //   }

      //   /// <summary>
      //   /// Throws OperationCanceledException if token cancels before the real task completes.
      //   /// Doesn't abort the inner task, but allows the calling code to get "unblocked" and react to stuck tasks.
      //   /// </summary>
      //   public static Task WithCancellationAsync(this Task task, CancellationToken token, bool useSynchronizationContext = false, bool continueOnCapturedContext = false) {
      //      if (!token.CanBeCanceled || task.IsCompleted) {
      //         return task;
      //      }
      //      else if (token.IsCancellationRequested) {
      //         return Task.FromCanceled(token);
      //      }

      //      return Inner(task, token, useSynchronizationContext, continueOnCapturedContext);

      //      static async Task Inner(Task task, CancellationToken token, bool useSynchronizationContext, bool continueOnCapturedContext) {
      //         var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      //         using (token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs, useSynchronizationContext)) {
      //            if (task != await Task.WhenAny(task, tcs.Task)) {
      //               token.ThrowIfCancellationRequested();
      //            }
      //         }

      //         // This is needed to re-throw eventual task exception that would be grouped instead.
      //         await task.ConfigureAwait(continueOnCapturedContext);
      //      }
      //   }

      /// <summary>
      /// Allows to cancel awaitable operations with a cancellationToken.
      /// https://devblogs.microsoft.com/pfxteam/how-do-i-cancel-non-cancelable-async-operations/
      /// </summary>
      /// <typeparam name="T">Task return type</typeparam>
      /// <param name="task">The task.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      /// <exception cref="OperationCanceledException">Task has been canceled.</exception>
      public static async Task<T> WithCancellationAsync<T>(this Task<T> task, CancellationToken cancellationToken) {
         var tcs = new TaskCompletionSource<bool>();

         using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)) {
               throw new OperationCanceledException(cancellationToken);
            }
         }

         return await task.ConfigureAwait(false);
      }

      /// <summary>
      /// Allows to cancel awaitable operations with a cancellationToken.
      /// https://devblogs.microsoft.com/pfxteam/how-do-i-cancel-non-cancelable-async-operations/
      /// </summary>
      /// <param name="task">The task.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      /// <exception cref="OperationCanceledException">Task has been canceled.</exception>
      public static async Task WithCancellationAsync(this Task task, CancellationToken cancellationToken) {
         var tcs = new TaskCompletionSource<bool>();

         using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)) {
               throw new OperationCanceledException(cancellationToken);
            }
         }

         await task.ConfigureAwait(false); // This is needed to re-throw eventual task exception.
      }
   }
}