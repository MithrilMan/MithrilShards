using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Threading;

namespace MithrilShards.Example.Network.Client
{
   public class ExampleRequiredConnection : ConnectorBase
   {
      private const int INNER_DELAY = 500;

      private ExampleSettings settings;

      private readonly List<OutgoingConnectionEndPoint> connectionsToAttempt = new List<OutgoingConnectionEndPoint>();

      public ExampleRequiredConnection(ILogger<ExampleRequiredConnection> logger,
                                IEventBus eventBus,
                                IOptions<ExampleSettings> options,
                                IConnectivityPeerStats serverPeerStats,
                                IForgeConnectivity forgeConnectivity,
                                IPeriodicWork connectionLoop) : base(logger, eventBus, serverPeerStats, forgeConnectivity, connectionLoop)
      {
         this.settings = options.Value!;

         foreach (ExampleClientPeerBinding peerBinding in this.settings.Connections)
         {
            if (!peerBinding.TryGetExampleEndPoint(out ExampleEndPoint? endPoint))
            {
               logger.LogWarning("Required connection skipped because of wrong format, check settings file. {Endpoint}", peerBinding.EndPoint);
               continue;
            }

            var remoteEndPoint = new OutgoingConnectionEndPoint(endPoint);
            remoteEndPoint.Items[nameof(endPoint.MyExtraInformation)] = endPoint.MyExtraInformation;
            this.connectionsToAttempt.Add(remoteEndPoint);
         }
      }

      protected override async ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation)
      {
         foreach (OutgoingConnectionEndPoint endPoint in this.connectionsToAttempt)
         {
            if (cancellation.IsCancellationRequested) break;

            if (connectionManager.CanConnectTo(endPoint.EndPoint))
            {
               // note that AttemptConnection is not blocking because it returns when the peer fails to connect or when one of the parties disconnect
               _ = this.forgeConnectivity.AttemptConnectionAsync(endPoint, cancellation).ConfigureAwait(false);

               // apply a delay between attempts to prevent too many connection attempt in a row
               await Task.Delay(INNER_DELAY).ConfigureAwait(false);
            }
         }
      }
   }
}
