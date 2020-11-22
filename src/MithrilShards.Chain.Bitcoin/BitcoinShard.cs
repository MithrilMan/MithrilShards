using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Chain.Bitcoin
{
   public class BitcoinShard : IMithrilShard
   {
      private readonly ILogger<BitcoinShard> _logger;
      private readonly BitcoinSettings _settings;

      public BitcoinShard(ILogger<BitcoinShard> logger, IOptions<BitcoinSettings> settings)
      {
         this._logger = logger;
         this._settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
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
