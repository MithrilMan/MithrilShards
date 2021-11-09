using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Statistics;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Network;

public class BitcoinConnectionManager : ConnectionManager
{
   private readonly TimeSpan _inactivityThreshold = TimeSpan.FromSeconds(2 * 60);
   readonly IRandomNumberGenerator _randomNumberGenerator;
   readonly IPeriodicWork _periodicPeerHealthCheck;

   public BitcoinConnectionManager(ILogger<ConnectionManager> logger, IEventBus eventBus,
                                   IStatisticFeedsCollector statisticFeedsCollector,
                                   IEnumerable<IConnector> connectors,
                                   IRandomNumberGenerator randomNumberGenerator,
                                   IPeriodicWork periodicPeerHealthCheck) : base(logger, eventBus, statisticFeedsCollector, connectors)
   {
      _randomNumberGenerator = randomNumberGenerator;
      _periodicPeerHealthCheck = periodicPeerHealthCheck;
   }

   public override Task StartAsync(CancellationToken cancellationToken)
   {
      _ = _periodicPeerHealthCheck.StartAsync(
            label: nameof(_periodicPeerHealthCheck),
            work: StartCheckingPeerHealthAsync,
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
