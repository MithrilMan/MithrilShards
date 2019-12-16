using System.Threading;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Allows consumers to perform cleanup during a graceful shutdown.
   /// </summary>
   public interface IForgeLifetime {
      ForgeState State { get; }

      /// <summary>
      /// Triggered when the forge is performing a graceful shutdown.
      /// Requests may still be in flight. Shutdown will block until this event completes.
      /// </summary>
      CancellationToken ForgeShuttingDown { get; }

      /// <summary>
      /// Requests the termination of the current forge instance.
      /// </summary>
      void ShutDown();

      /// <summary>
      /// Sets the new <see cref="IForge"/> state.
      /// </summary>
      /// <param name="newState">The new <see cref="IForge"/> state.</param>
      void SetState(ForgeState newState);
   }
}