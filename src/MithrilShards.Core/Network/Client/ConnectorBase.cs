using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Core.Network.Client
{
   public abstract class ConnectorBase : IConnector, IPeriodicWorkExceptionHandler
   {
      protected ILogger logger;
      protected IEventBus eventBus;
      protected readonly IConnectivityPeerStats peerStats;
      protected readonly IForgeConnectivity forgeConnectivity;
      protected readonly IPeriodicWork connectionLoop;

      protected IConnectionManager? connectionManager;

      public TimeSpan DefaultDelayBetweenAttempts { get; protected set; }

      public ConnectorBase(ILogger logger,
                           IEventBus eventBus,
                           IConnectivityPeerStats serverPeerStats,
                           IForgeConnectivity forgeConnectivity,
                           IPeriodicWork connectionLoop)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         peerStats = serverPeerStats;
         this.forgeConnectivity = forgeConnectivity;
         this.connectionLoop = connectionLoop;

         DefaultDelayBetweenAttempts = TimeSpan.FromSeconds(15);

         this.connectionLoop.Configure(stopOnException: true, exceptionHandler: this);
      }

      public void SetConnectionManager(IConnectionManager connectionManager)
      {
         this.connectionManager = connectionManager;
      }

      /// <summary>
      /// Contains the logic to compute the delay adjustment.
      /// </summary>
      /// <returns></returns>
      /// <remarks>
      /// Override this method to have a custom logic for delayed connection attempts.
      /// </remarks>
      public virtual TimeSpan ComputeDelayAdjustment()
      {
         return DefaultDelayBetweenAttempts;
      }

      /// <summary>
      /// Task that runs until the application ends (or the passed cancellation token is canceled).
      /// </summary>
      /// <param name="cancellation"></param>
      public virtual async Task StartConnectionLoopAsync(CancellationToken cancellation)
      {
         await connectionLoop.StartAsync(
              label: "connectionLoop",
              work: ConnectionLoopAsync,
              ComputeDelayAdjustment,
              cancellation
           ).ConfigureAwait(false);
      }

      private async Task ConnectionLoopAsync(CancellationToken cancellation)
      {
         if (connectionManager == null)
         {
            ThrowHelper.ThrowNullReferenceException($"{nameof(connectionManager)} cannot be null.");
         }

         await AttemptConnectionsAsync(connectionManager, cancellation).ConfigureAwait(false);
      }

      /// <summary>
      /// Attempts to perform connections to peers applying implementation logic.
      /// </summary>
      /// <param name="connectionManager">The connection manager.</param>
      /// <param name="cancellation">The cancellation.</param>
      /// <returns></returns>
      protected abstract ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation);

      public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         if (failedWork == connectionLoop)
         {
            logger.LogCritical(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", GetType().Name);
            feedback.ContinueExecution = false;
            feedback.IsCritical = true;
            feedback.Message = "Without Connector loop no new connection can be established, restart the node to fix the problem.";
            return;
         }

         feedback.ContinueExecution = true;
      }
   }
}