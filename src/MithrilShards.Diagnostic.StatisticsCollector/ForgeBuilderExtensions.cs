using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <param name="minimumSupportedVersion">The minimum version local nodes requires in order to connect to other peers.</param>
      /// <param name="currentVersion">The current version local peer aim to use with connected peers.</param>
      /// <returns></returns>
      public static IForgeBuilder UseStatisticsCollector(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<StatisticsCollectorShard>(
            (hostBuildContext, services) =>
            {
               services
                  .AddSingleton<StatisticFeedsCollector>()
                  .AddSingleton<IStatisticFeedsCollector>(sp => sp.GetRequiredService<StatisticFeedsCollector>())
                  .AddSingleton<IHostedService>(sp => sp.GetRequiredService<StatisticFeedsCollector>())
                  ;
            });

         return forgeBuilder;
      }
   }
}