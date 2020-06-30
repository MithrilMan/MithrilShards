using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Statistics;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public class BitcoinConnectionManager : ConnectionManager
   {
      private readonly TimeSpan inactivityThreshold = TimeSpan.FromSeconds(2 * 60);
      readonly IRandomNumberGenerator randomNumberGenerator;
      readonly IPeriodicWork periodicPeerHealthCheck;

      public BitcoinConnectionManager(ILogger<ConnectionManager> logger, IEventBus eventBus,
                                      IStatisticFeedsCollector statisticFeedsCollector,
                                      IEnumerable<IConnector> connectors,
                                      IRandomNumberGenerator randomNumberGenerator,
                                      IPeriodicWork periodicPeerHealthCheck) : base(logger, eventBus, statisticFeedsCollector, connectors)
      {
         this.randomNumberGenerator = randomNumberGenerator;
         this.periodicPeerHealthCheck = periodicPeerHealthCheck;
      }

      public override Task StartAsync(CancellationToken cancellationToken)
      {
         _ = this.periodicPeerHealthCheck.StartAsync(
               label: $"periodicPeerHealthCheck",
               work: this.StartCheckingPeerHealthAsync,
               interval: TimeSpan.FromSeconds(10),
               cancellation: cancellationToken
            );

         return base.StartAsync(cancellationToken);
      }

      private Task StartCheckingPeerHealthAsync(CancellationToken cancellationToken)
      {
         //DateTimeOffset dateReference = DateTimeOffset.UtcNow;

         //foreach (KeyValuePair<string, IPeerContext> peerItem in this.inboundPeers)
         //{
         //   var peer = peerItem.Value;

         //   if (peer.Metrics.LastActivity - dateReference < this.inactivityThreshold)
         //   {
         //      var ping = new PingMessage
         //      {
         //         Nonce = this.randomNumberGenerator.GetUint64()
         //      };

         //      await peer.GetMessageWriter().WriteAsync(ping, cancellationToken).ConfigureAwait(false);
         //   }
         //}

         // ping is handled in the PingPongProcessor
         return Task.CompletedTask;
      }
   }
}