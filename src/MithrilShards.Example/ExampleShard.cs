using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Example
{
   public class ExampleShard : IMithrilShard
   {
      private readonly ILogger<ExampleShard> logger;
      private readonly ExampleSettings settings;

      public ExampleShard(ILogger<ExampleShard> logger, IOptions<ExampleSettings> settings)
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
