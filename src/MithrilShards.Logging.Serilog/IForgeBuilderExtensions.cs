using Microsoft.Extensions.Configuration;
using MithrilShards.Core.Forge;
using Serilog;

namespace MithrilShards.Logging.Serilog
{
   public static class IForgeBuilderExtension
   {
      public static IForgeBuilder UseSerilog(this IForgeBuilder forgeBuilder, string configurationFile = null)
      {
         forgeBuilder
            .ExtendInnerHostBuilder(builder =>
            {

               builder.UseSerilog((hostingContext, loggerConfiguration) =>
               {
                  IConfigurationRoot logConfiguration = new ConfigurationBuilder()
                  .AddJsonFile(configurationFile ?? forgeBuilder.ConfigurationFileName)
                  .Build();

                  loggerConfiguration.ReadFrom.Configuration(logConfiguration);
               });
            });
         return forgeBuilder;
      }
   }
}
