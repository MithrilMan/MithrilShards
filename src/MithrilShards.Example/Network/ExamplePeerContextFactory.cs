using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Example.Network
{
   public class ExamplePeerContextFactory : PeerContextFactory<ExamplePeerContext>
   {
      public ExamplePeerContextFactory(ILogger<ExamplePeerContextFactory> logger, IEventBus eventBus, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings)
         : base(logger, eventBus, loggerFactory, serverSettings)
      {
      }
   }
}
