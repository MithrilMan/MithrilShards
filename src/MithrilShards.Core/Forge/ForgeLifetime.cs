using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Forge {

   /// <summary>
   /// Allows consumers to perform cleanup during a graceful shutdown.
   /// Borrowed from asp.net core
   /// </summary>
   public class ForgeLifetime : IForgeLifetime {
      public ForgeState State { get; private set; }

      private readonly CancellationTokenSource ForgeShuttingDownSource = new CancellationTokenSource();
      private readonly IEventBus eventBus;
      private readonly ILogger logger;

      /// <summary>
      /// Triggered when the application host is performing a graceful shutdown.
      /// Request may still be in flight. Shutdown will block until this event completes.
      /// </summary>
      public CancellationToken ForgeShuttingDown {
         get {
            return this.ForgeShuttingDownSource.Token;
         }
      }

      public ForgeLifetime(ILogger<ForgeLifetime> logger, IEventBus eventBus) {
         this.logger = logger;
         this.eventBus = eventBus;
      }

      /// <summary>
      /// Signals the ApplicationStopping event and blocks until it completes.
      /// </summary>
      public void ShutDown() {
         CancellationTokenSource stoppingSource = this.ForgeShuttingDownSource;
         bool lockTaken = false;
         try {
            Monitor.Enter((object)stoppingSource, ref lockTaken);
            try {
               this.ForgeShuttingDownSource.Cancel(false);
            }
            catch (Exception) {
            }
         }
         finally {
            if (lockTaken) {
               Monitor.Exit((object)stoppingSource);
            }
         }
      }

      public void SetState(ForgeState newState) {
         if (newState == ForgeState.Starting) {
            if (this.State == ForgeState.ShuttingDown || this.State == ForgeState.ShuttedDown) {
               throw new ObjectDisposedException(nameof(Forge));
            }
         }

         this.State = newState;

         switch (this.State) {
            case ForgeState.Created:
               this.eventBus.Publish(new Events.ForgeCreated());
               break;
            case ForgeState.Starting:
               this.eventBus.Publish(new Events.ForgeStarting());
               break;
            case ForgeState.Started:
               this.eventBus.Publish(new Events.ForgeStarted());
               break;
            case ForgeState.ShuttingDown:
               this.eventBus.Publish(new Events.ForgeShuttingDown());
               break;
            case ForgeState.ShuttedDown:
               this.eventBus.Publish(new Events.ForgeShuttedDown());
               break;
            default:
               throw new ArgumentException("Unknown Forge state", nameof(this.State));
         }

         this.logger.LogInformation("Forge status changed: {ForgeState}", this.State);
      }
   }
}
