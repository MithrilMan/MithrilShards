using Microsoft.Extensions.Configuration;
using MithrilShards.Core.Forge;
using Serilog;

namespace MithrilShards.Logging.Serilog
{
   public static class IForgeBuilderExtension
   {
      public static IForgeBuilder UseSerilog(this IForgeBuilder forgeBuilder, string? configurationFile = null)
      {
         forgeBuilder
            .ExtendInnerHostBuilder(builder =>
            {

               builder.UseSerilog((hostingContext, loggerConfiguration) =>
               {

                  IConfigurationRoot logConfiguration = new ConfigurationBuilder()
                  .AddJsonFile(configurationFile ?? forgeBuilder.ConfigurationFileName, false, true)
                  .SetFileLoadExceptionHandler(fileContext =>
                  {
                     //set default logging if the log file is missing
                     loggerConfiguration
                        .MinimumLevel.Warning()
                        .WriteTo.Console();

                     using global::Serilog.Core.Logger logger = new LoggerConfiguration()
                        .MinimumLevel.Warning()
                        .WriteTo.Console()
                        .CreateLogger();

                     logger.Warning("Missing log configuration file {MissingLogFileName}, using console and warning level", fileContext.Provider.Source.Path);

                     fileContext.Ignore = true;
                  })
                  .Build();

                  loggerConfiguration.ReadFrom.Configuration(logConfiguration);
               });
            });
         return forgeBuilder;
      }
   }
}
