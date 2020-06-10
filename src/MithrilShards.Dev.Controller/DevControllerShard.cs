using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerShard : IMithrilShard
   {
      readonly ILogger<DevControllerShard> logger;
      readonly IOptions<DevControllerSettings> options;

      public DevControllerShard(ILogger<DevControllerShard> logger, IOptions<DevControllerSettings> options)
      {
         this.logger = logger;
         this.options = options;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         this.logger.LogDebug("DevControllerShard listening to {DevControllerShardEndpoint}", this.options.Value?.EndPoint);
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         return default;
      }
   }
}
