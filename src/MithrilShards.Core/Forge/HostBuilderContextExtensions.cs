using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Forge;

public static class HostBuilderContextExtensions
{
   public static TMithrilShardSettings? GetShardSettings<TMithrilShardSettings>(this HostBuilderContext context) where TMithrilShardSettings : class, IMithrilShardSettings
   {
      TMithrilShardSettings? settings = null;
      var key = typeof(TMithrilShardSettings);

      if (context.Properties.TryGetValue(key, out object? value))
      {
         settings = value as TMithrilShardSettings;
      }

      return settings;
   }

   internal static TMithrilShardSettings SetShardSettings<TMithrilShardSettings>(this HostBuilderContext context, IServiceCollection services) where TMithrilShardSettings : class, IMithrilShardSettings
   {
      var key = typeof(TMithrilShardSettings);

      TMithrilShardSettings? settings;
      if (!context.Properties.TryGetValue(key, out object? value))
      {
         var tempSP = services.BuildServiceProvider();
         settings = tempSP.GetRequiredService<IOptions<TMithrilShardSettings>>().Value;
         context.Properties[key] = settings;
      }
      else
      {
         settings = (TMithrilShardSettings)value;
      }

      return settings;
   }
}
