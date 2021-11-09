using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Forge;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MithrilShards.Logging.Serilog;

public static class IForgeBuilderExtension
{
   public static IForgeBuilder UseSerilog(this IForgeBuilder forgeBuilder, string? configurationFile = null)
   {
      LevelSwitcherManager levelSwitcherManager = new LevelSwitcherManager();

      forgeBuilder.AddShard<SerilogShard>((builder, services) =>
      {
         services.AddSingleton<LevelSwitcherManager>(levelSwitcherManager);
      });

      forgeBuilder.ExtendInnerHostBuilder(builder =>
      {
            /// Add and configure Serilog
            builder.UseSerilog((hostingContext, loggerConfiguration) =>
         {
            configurationFile ??= forgeBuilder.ConfigurationFileName;

            string absoluteDirectoryPath = Path.GetDirectoryName(Path.GetFullPath(configurationFile))!;
            var configurationFileProvider = new PhysicalFileProvider(absoluteDirectoryPath);

            IConfigurationRoot logConfiguration = new ConfigurationBuilder()
            .AddJsonFile(configurationFileProvider, Path.GetFileName(configurationFile), false, true)
            .SetFileLoadExceptionHandler(fileContext =>
            {
               using Logger logger = new LoggerConfiguration()
                  .MinimumLevel.Warning()
                  .WriteTo.Console()
                  .CreateLogger();

               logger.Warning("Missing log configuration file {MissingLogFileName}, using console and Information level", fileContext.Provider.Source.Path);

               fileContext.Ignore = true;
            })
            .Build();

            loggerConfiguration.ReadFrom.Configuration(logConfiguration);
            if (!logConfiguration.GetSection("Serilog").Exists())
            {
                  //set default logging if the log file is missing
                  loggerConfiguration
               .MinimumLevel.Information()
               .WriteTo.Console();
            }
            else
            {
                  //create log level switch
                  levelSwitcherManager.LoadLoggingLevelSwitches(logConfiguration);
               levelSwitcherManager.BindLevelSwitches(loggerConfiguration);
            }
         });
      });
      return forgeBuilder;
   }
}
