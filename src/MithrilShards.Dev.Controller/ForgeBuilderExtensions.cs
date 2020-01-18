using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MithrilShards.Core.Forge;

namespace MithrilShards.Dev.Controller
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
      public static IForgeBuilder UseDevController(this IForgeBuilder forgeBuilder, string? configurationFile = null)
      {
         forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>(
            (hostBuildContext, services) =>
            {
               //services
               //   .AddSingleton<StatisticFeedsCollector>()
               //   .AddSingleton<IStatisticFeedsCollector>(sp => sp.GetRequiredService<StatisticFeedsCollector>())
               //   .AddSingleton<IHostedService>(sp => sp.GetRequiredService<StatisticFeedsCollector>())
               //   ;
            });


         forgeBuilder.ExtendInnerHostBuilder(builder =>
         {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                  .AddJsonFile(configurationFile ?? forgeBuilder.ConfigurationFileName)
                  .Build();

            var settings = new DevControllerSettings();
            configuration.Bind(settings.ConfigurationSection, settings);

            if (!IPEndPoint.TryParse(settings.EndPoint, out IPEndPoint iPEndPoint))
            {
               throw new ArgumentException($"Wrong configuration parameter for {nameof(settings.EndPoint)}");
            }

            builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureWebHost(webBuilder =>
            {
               webBuilder
                  .UseKestrel(serverOptions =>
                  {
                     serverOptions.Listen(iPEndPoint);
                  })
                  .UseStartup<Startup>();
            });
         });

         return forgeBuilder;
      }
   }
}