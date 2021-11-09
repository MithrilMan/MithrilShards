using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Diagnostic.StatisticsCollector;

public static class ForgeBuilderExtensions
{
   /// <summary>
   /// Uses the bitcoin chain.
   /// </summary>
   /// <param name="forgeBuilder">The forge builder.</param>
   /// <param name="configuration">The action to configure the collector.</param>
   /// <returns></returns>
   public static IForgeBuilder UseStatisticsCollector(this IForgeBuilder forgeBuilder, Action<StatisticsCollectorOptions>? configuration = null)
   {
      var options = new StatisticsCollectorOptions();
      configuration?.Invoke(options);

      forgeBuilder.AddShard<StatisticsCollectorShard, StatisticsCollectorSettings>(
         (hostBuildContext, services) =>
         {
            services
               .AddSingleton<IStatisticFeedsCollector, StatisticFeedsCollector>()
               .AddHostedService(sp => sp.GetRequiredService<IStatisticFeedsCollector>())
               ;

            if (options.DumpOnConsoleOnKeyPress == true)
            {
               services.AddSingleton<ConsoleKeyDumper>();
            }
         });

      return forgeBuilder;
   }
}

public class StatisticsCollectorOptions
{
   /// <summary>
   /// Gets or sets a value indicating whether to dump statistics on console on S key pressed.
   /// </summary>
   public bool DumpOnConsoleOnKeyPress { get; set; } = false;
}
