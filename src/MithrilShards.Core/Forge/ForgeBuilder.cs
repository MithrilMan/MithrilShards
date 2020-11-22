using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge
{
   public class ForgeBuilder : IForgeBuilder
   {
      const string CONFIGURATION_FILE = "forge-settings.json";

      /// <summary>
      /// A temporary console logger that can be used to report to user errors that may happens for example with missing configuration files and we don't have yet proper logger registration.
      /// </summary>
      private readonly ILogger<ForgeBuilder> _logger;
      private bool _isForgeSet = false;
      private bool _createDefaultConfigurationFileNeeded = false;

      public readonly HostBuilder hostBuilder;
      public string ConfigurationFileName { get; private set; } = null!; //set to something meaningful during initialization

      public ForgeBuilder()
      {
         // create a temporary logger that logs on console to communicate pre-initialization errors that may happens for example with missing configuration files
         ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Warning).AddConsole());
         _logger = loggerFactory.CreateLogger<ForgeBuilder>();

         hostBuilder = new HostBuilder();

         // Add a new service provider configuration
         hostBuilder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseDefaultServiceProvider((context, options) =>
            {
               options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
               options.ValidateOnBuild = true;
            });
      }

      /// <summary>
      /// Creates the default configuration file if it's missing.
      /// </summary>
      /// <returns></returns>
      private void CreateDefaultConfigurationFile(FileLoadExceptionContext fileContext)
      {
         _createDefaultConfigurationFileNeeded = true;

         _logger.LogWarning($"Missing configuration file {ConfigurationFileName}, creating one with default values.");

         //default file created, no need to throw error
         fileContext.Ignore = true;
      }

      public IForgeBuilder UseForge<TForgeImplementation>(string[] commandLineArgs, string configurationFile = "forge-settings.json") where TForgeImplementation : class, IForge
      {
         if (_isForgeSet)
         {
            throw new Exception($"Forge already set. Only one call to {nameof(UseForge)} is allowed");
         }

         _ = hostBuilder.ConfigureServices((context, services) =>
         {
            if (_createDefaultConfigurationFileNeeded)
            {
               services.AddSingleton<DefaultConfigurationWriter>(services =>
               {
                  return new DefaultConfigurationWriter(
                     services.GetService<ILoggerFactory>().CreateLogger<DefaultConfigurationWriter>(),
                     services.GetServices<IMithrilShardSettings>(),
                     ConfigurationFileName
                     );
               });
            }

            services
               .AddOptions()
               .AddSingleton<IServiceCollection>(services) // register forge service collection in order to create other sandboxed serviceProviders in other Hosts (e.g. for API purpose)
               .AddSingleton<IForge, TForgeImplementation>()
               .AddHostedService<TForgeImplementation>(serviceProvider => (TForgeImplementation)serviceProvider.GetRequiredService<IForge>())
               .ConfigureForge(context);
         });

         _isForgeSet = true;

         Configure(commandLineArgs, configurationFile);

         return this;
      }


      //
      // Summary:
      //     Adds a delegate for configuring the provided Microsoft.Extensions.Logging.ILoggingBuilder.
      //     This may be called multiple times.
      //
      // Parameters:
      //   hostBuilder:
      //     The Microsoft.Extensions.Hosting.IHostBuilder to configure.
      //
      //   configureLogging:
      //     The delegate that configures the Microsoft.Extensions.Logging.ILoggingBuilder.
      //
      // Returns:
      //     The same instance of the Microsoft.Extensions.Hosting.IHostBuilder for chaining.
      public IForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging)
      {
         hostBuilder.ConfigureLogging(configureLogging);
         return this;
      }

      public IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings, new()
      {

         AddShard<TMithrilShard>(configureDelegate);

         //register shard configuration settings
         hostBuilder.ConfigureServices((context, services) =>
         {
            services.Configure<TMithrilShardSettings>(MithrilShardSettingsManager.GetSection<TMithrilShardSettings>(context.Configuration));

            //register the shard configuration setting as IMithrilShardSettings in order to allow DefaultConfigurationWriter to write default its default values
            services.AddSingleton<IMithrilShardSettings, TMithrilShardSettings>();
         });

         return this;
      }

      public IForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard
      {

         if (configureDelegate is null)
         {
            throw new ArgumentNullException(nameof(configureDelegate));
         }

         hostBuilder.ConfigureServices((context, services) =>
         {
            services.AddSingleton<IMithrilShard, TMithrilShard>();
            configureDelegate(context, services);
         });

         return this;
      }

      public IForgeBuilder ExtendInnerHostBuilder(Action<IHostBuilder> extendHostBuilderAction)
      {
         extendHostBuilderAction(hostBuilder);
         return this;
      }

      /// <summary>
      /// Adds the console log reading settings from Logging section if configuration file and displaying on standard console
      /// </summary>
      /// <returns></returns>
      public IForgeBuilder AddConsoleLog()
      {
         ConfigureLogging((context, logging) =>
         {
            logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            logging.AddConsole();
         });

         return this;
      }

      //
      // Summary:
      //     Enables console support, builds and starts the host, and waits for Ctrl+C or
      //     SIGTERM to shut down.
      //
      // Parameters:
      //   hostBuilder:
      //     The Microsoft.Extensions.Hosting.IHostBuilder to configure.
      //
      //   cancellationToken:
      //     A System.Threading.CancellationToken that can be used to cancel the console.
      //
      // Returns:
      //     A System.Threading.Tasks.Task that only completes when the token is triggered or
      //     shutdown is triggered.
      public Task RunConsoleAsync(CancellationToken cancellationToken = default)
      {
         EnsureForgeIsSet();

         return hostBuilder.RunConsoleAsync(cancellationToken);
      }

      private void EnsureForgeIsSet()
      {
         if (!_isForgeSet)
         {
            throw new ForgeBuilderException("Forge must be set. A call to UseForge is required");
         }
      }

      private IForgeBuilder Configure(string[] commandLineArgs, string configurationFile = CONFIGURATION_FILE)
      {
         ConfigurationFileName = Path.GetFullPath(configurationFile ?? CONFIGURATION_FILE);
         string absoluteDirectoryPath = Path.GetDirectoryName(ConfigurationFileName)!;
         if (!Directory.Exists(absoluteDirectoryPath))
         {
            _logger.LogWarning($"Creating directory structure to store configuration file {ConfigurationFileName}.");
            Directory.CreateDirectory(absoluteDirectoryPath);
         }
         var configurationFileProvider = new PhysicalFileProvider(absoluteDirectoryPath);

         _ = hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
         {
            // do not change optional to true, because there is SetFileLoadExceptionHandler that will create a default file if missing.
            config.AddJsonFile(configurationFileProvider, Path.GetFileName(ConfigurationFileName), optional: false, reloadOnChange: true);

            config.AddEnvironmentVariables("FORGE_");

            if (commandLineArgs != null)
            {
               config.AddCommandLine(commandLineArgs);
            }

            config.SetFileLoadExceptionHandler(CreateDefaultConfigurationFile);
         });

         return this;
      }
   }
}
