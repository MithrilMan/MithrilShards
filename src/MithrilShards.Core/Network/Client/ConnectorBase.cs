using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Core.Network.Client
{
   public abstract class ConnectorBase : IConnector, IPeriodicWorkExceptionHandler
   {
      protected ILogger<RequiredConnection> logger;
      protected IEventBus eventBus;
      protected readonly IConnectivityPeerStats peerStats;
      readonly IPeriodicWork connectionLoop;
      protected readonly ForgeConnectivitySettings settings;

      protected IConnectionManager? connectionManager;

      public TimeSpan DefaultDelayBetweenAttempts { get; protected set; }

      public ConnectorBase(ILogger<RequiredConnection> logger,
                           IEventBus eventBus,
                           IOptions<ForgeConnectivitySettings> options,
                           IConnectivityPeerStats serverPeerStats,
                           IPeriodicWork connectionLoop)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerStats = serverPeerStats;
         this.connectionLoop = connectionLoop;
         this.settings = options.Value;

         this.DefaultDelayBetweenAttempts = TimeSpan.FromSeconds(15);

         this.connectionLoop.Configure(
            stopOnException: true,
            exceptionHandler: this
            );
      }

      public void SetConnectionManager(IConnectionManager connectionManager)
      {
         this.connectionManager = connectionManager;
      }

      /// <summary>
      /// Contains the logic to compute the delay adjustment.
      /// </summary>
      /// <param name="hint">The hint.</param>
      /// <remarks>Override this method to have a custom logic for delayed connection attempts.</remarks>
      /// <returns></returns>
      public virtual TimeSpan ComputeDelayAdjustment()
      {
         return this.DefaultDelayBetweenAttempts;
      }

      /// <summary>
      /// Task that runs until the application ends (or the passed cancellation token is canceled).
      /// </summary>
      /// <param name="cancellation"></param>
      public virtual async Task StartConnectionLoopAsync(CancellationToken cancellation)
      {
         await this.connectionLoop.StartAsync(
              label: "connectionLoop",
              work: ConnectionLoopAsync,
              this.ComputeDelayAdjustment,
              cancellation
           ).ConfigureAwait(false);
      }

      private async Task ConnectionLoopAsync(CancellationToken cancellation)
      {
         if (this.connectionManager == null)
         {
            ThrowHelper.ThrowNullReferenceException($"{nameof(this.connectionManager)} cannot be null.");
         }

         await this.AttemptConnectionsAsync(this.connectionManager, cancellation).ConfigureAwait(false);
      }

      /// <summary>
      /// Attempts to perform connections to peers applying implementation logic.
      /// </summary>
      /// <param name="connectionManager">The connection manager.</param>
      /// <param name="cancellation">The cancellation.</param>
      /// <returns></returns>
      protected abstract ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation);

      public void OnException(IPeriodicWork failedWork, Exception ex, out bool continueExecution)
      {
         if (failedWork == this.connectionLoop)
         {
            this.logger.LogCritical(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", this.GetType().Name);
            continueExecution = false;
            return;
         }

         continueExecution = true;
      }
   }
}