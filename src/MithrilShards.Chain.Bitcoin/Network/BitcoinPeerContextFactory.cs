using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network;

public class BitcoinPeerContextFactory : PeerContextFactory<BitcoinPeerContext>
{
   public BitcoinPeerContextFactory(ILogger<PeerContextFactory<BitcoinPeerContext>> logger, IEventBus eventBus, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings)
      : base(logger, eventBus, loggerFactory, serverSettings)
   {
   }
}
