using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Network.Client
{
   public abstract class ConnectorBase : IConnector
   {
      protected ILogger<RequiredConnection> logger;
      protected IEventBus eventBus;
      protected readonly IConnectivityPeerStats peerStats;
      protected readonly ForgeConnectivitySettings settings;

      public TimeSpan DefaultDelayBetweenAttempts { get; protected set; }

      public ConnectorBase(ILogger<RequiredConnection> logger, IEventBus eventBus, IOptions<ForgeConnectivitySettings> options, IConnectivityPeerStats serverPeerStats)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerStats = serverPeerStats;
         this.settings = options.Value;

         this.DefaultDelayBetweenAttempts = TimeSpan.FromSeconds(15);
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
      public virtual async Task StartConnectionLoopAsync(IConnectionManager connectionManager, CancellationToken cancellation)
      {
         while (!cancellation.IsCancellationRequested)
         {
            try
            {
               await this.AttemptConnectionsAsync(connectionManager, cancellation).ConfigureAwait(false);
               await Task.Delay(this.ComputeDelayAdjustment(), cancellation).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
               this.logger.LogDebug("Connector {Connector} canceled.", this.GetType().Name);
               break;
            }
            catch (Exception ex)
            {
               this.logger.LogError(ex, "Connector {Connector} failure, it has been stopped, node may have connection problems.", this.GetType().Name);
               break;
            }
         }
      }


      /// <summary>
      /// Attempts to perform connections to peers applying implementation logic.
      /// </summary>
      /// <param name="connectionManager">The connection manager.</param>
      /// <param name="cancellation">The cancellation.</param>
      /// <returns></returns>
      protected abstract ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation);
   }
}