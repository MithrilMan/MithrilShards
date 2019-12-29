using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.MithrilShards;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Chain.Bitcoin {
   public class BitcoinShard : IMithrilShard {
      private readonly ILogger<BitcoinShard> logger;
      private readonly BitcoinSettings settings;

      public BitcoinShard(ILogger<BitcoinShard> logger, IOptions<BitcoinSettings> settings) {
         this.logger = logger;
         this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
      }

      public Task InitializeAsync(CancellationToken cancellationToken) {
         return Task.CompletedTask;
      }

      public Task StartAsync(CancellationToken cancellationToken) {
         // NOP
         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken) {
         // NOP
         return Task.CompletedTask;
      }
   }
}
