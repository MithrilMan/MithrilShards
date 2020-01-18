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
      private readonly ILogger<BitcoinShard> logger;
      private readonly BitcoinSettings settings;

      public BitcoinShard(ILogger<BitcoinShard> logger, IOptions<BitcoinSettings> settings)
      {
         this.logger = logger;
         this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
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
