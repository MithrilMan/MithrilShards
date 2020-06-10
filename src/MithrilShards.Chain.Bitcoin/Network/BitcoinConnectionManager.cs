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

namespace MithrilShards.Chain.Bitcoin.Network
{
   public class BitcoinConnectionManager : ConnectionManager
   {
      private readonly TimeSpan inactivityThreshold = TimeSpan.FromSeconds(2 * 60);
      readonly IRandomNumberGenerator randomNumberGenerator;

      public BitcoinConnectionManager(ILogger<ConnectionManager> logger, IEventBus eventBus,
                                      IStatisticFeedsCollector statisticFeedsCollector,
                                      IEnumerable<IConnector> connectors,
                                      IRandomNumberGenerator randomNumberGenerator) : base(logger, eventBus, statisticFeedsCollector, connectors)
      {
         this.randomNumberGenerator = randomNumberGenerator;
      }

      public override Task StartAsync(CancellationToken cancellationToken)
      {
         _ = this.StartCheckingPeerHealthAsync(cancellationToken);

         return base.StartAsync(cancellationToken);
      }

      private async Task StartCheckingPeerHealthAsync(CancellationToken cancellationToken)
      {
         while (!cancellationToken.IsCancellationRequested)
         {
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            DateTimeOffset dateReference = DateTimeOffset.UtcNow;

            foreach (KeyValuePair<string, IPeerContext> peer in this.inboundPeers)
            {
               if (peer.Value.Metrics.LastActivity - dateReference < this.inactivityThreshold)
               {
                  var ping = new PingMessage
                  {
                     Nonce = this.randomNumberGenerator.GetUint64()
                  };

                  await peer.Value.GetMessageWriter().WriteAsync(ping, cancellationToken).ConfigureAwait(false);
               }
            }
         }
      }
   }
}
