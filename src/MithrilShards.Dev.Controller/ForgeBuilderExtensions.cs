using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Statistics;

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
      public static IForgeBuilder UseDevController(this IForgeBuilder forgeBuilder, string configurationFile = null)
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

            builder.ConfigureWebHost(webBuilder =>
            {
               webBuilder
                  .CaptureStartupErrors(true)
                  .UseKestrel((context, serverOptions) =>
                  {
                     serverOptions.Listen(iPEndPoint);
                  // Set properties and call methods on options
               })
                  .Configure(app => app
                     //.UseMvc()
                     .UseSwagger()
                     .UseSwaggerUI(setup => setup.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev Controller"))
                     .UseRouting()
                     .UseEndpoints(endpoints =>
                     {
                        endpoints.MapControllers();
                     })
                     )
                     .ConfigureServices(services =>
                     {
                        services.AddControllers();
                        services.AddSwaggerGen(setup => setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Dev Controller", Version = "v1" }));
                     });
            });
         });


         return forgeBuilder;
      }
   }
}