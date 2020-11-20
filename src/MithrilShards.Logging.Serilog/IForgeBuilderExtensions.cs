using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
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
                  configurationFile ??= forgeBuilder.ConfigurationFileName;

                  string absoluteDirectoryPath = Path.GetDirectoryName(Path.GetFullPath(configurationFile))!;
                  var configurationFileProvider = new PhysicalFileProvider(absoluteDirectoryPath);

                  IConfigurationRoot logConfiguration = new ConfigurationBuilder()
                  .AddJsonFile(configurationFileProvider, Path.GetFileName(configurationFile), false, true)
                  .SetFileLoadExceptionHandler(fileContext =>
                  {
                     //set default logging if the log file is missing
                     loggerConfiguration
                        .MinimumLevel.Information()
                        .WriteTo.Console();

                     using global::Serilog.Core.Logger logger = new LoggerConfiguration()
                        .MinimumLevel.Warning()
                        .WriteTo.Console()
                        .CreateLogger();

                     logger.Warning("Missing log configuration file {MissingLogFileName}, using console and Information level", fileContext.Provider.Source.Path);

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
