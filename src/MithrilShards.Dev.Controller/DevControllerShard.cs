using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerShard : IMithrilShard
   {
      readonly ILogger<DevControllerShard> _logger;
      readonly DevControllerSettings _settings;

      public DevControllerShard(ILogger<DevControllerShard> logger, IOptions<DevControllerSettings> options)
      {
         _logger = logger;
         _settings = options.Value;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         if (!IPEndPoint.TryParse(_settings.EndPoint, out IPEndPoint iPEndPoint))
         {
            ThrowHelper.ThrowArgumentException($"Wrong configuration parameter for {nameof(_settings.EndPoint)}");
         }

         return default;
      }

      /// <inheritdoc/>
      public ValueTask StartAsync(CancellationToken cancellationToken) => default;

      /// <inheritdoc/>
      public ValueTask StopAsync(CancellationToken cancellationToken) => default;
   }
}
