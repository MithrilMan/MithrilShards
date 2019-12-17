using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge {
   public class ForgeBuilder : IForgeBuilder {
      const string CONFIGURATION_FILE = "forge.config2.json";

      private readonly HostBuilder hostBuilder;
      private bool isForgeSet = false;
      private bool createDefaultConfigurationFileNeeded = false;
      private string configurationFilePath;

      public ForgeBuilder() {
         this.hostBuilder = new HostBuilder();

         // Add a new service provider configuration
         this.hostBuilder.UseDefaultServiceProvider((context, options) => {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            options.ValidateOnBuild = true;
         });
      }


      public ForgeBuilder Configure(string[] commandLineArgs, string configurationFile = CONFIGURATION_FILE) {
         _ = this.hostBuilder.ConfigureAppConfiguration((hostingContext, config) => {

            this.configurationFilePath = configurationFile ?? CONFIGURATION_FILE;

            config.AddJsonFile(this.configurationFilePath, optional: false, reloadOnChange: true);

            config.AddEnvironmentVariables("FORGE_");

            if (commandLineArgs != null) {
               config.AddCommandLine(commandLineArgs);
            }

            config.SetFileLoadExceptionHandler(context => this.CreateDefaultConfigurationFile(hostingContext, context));
         });

         return this;
      }

      /// <summary>
      /// Creates the default configuration file if it's missing.
      /// </summary>
      /// <returns></returns>
      private void CreateDefaultConfigurationFile(HostBuilderContext hostingContext, FileLoadExceptionContext context) {
         this.createDefaultConfigurationFileNeeded = true;
         this.configurationFilePath = context.Provider.Source.Path;

         //default file created, no need to throw error
         context.Ignore = true;
      }

      public ForgeBuilder UseForge<TForgeImplementation>() where TForgeImplementation : class, IForge {
         if (this.isForgeSet) {
            throw new Exception($"Forge already set. Only one call to {nameof(UseForge)} is allowed");
         }

         _ = this.hostBuilder.ConfigureServices((context, services) => {

            if (this.createDefaultConfigurationFileNeeded) {
               services.AddSingleton<DefaultConfigurationWriter>(services => {
                  return new DefaultConfigurationWriter(
                     services.GetService<ILoggerFactory>().CreateLogger<DefaultConfigurationWriter>(),
                     services.GetServices<IMithrilShardSettings>(),
                     this.configurationFilePath
                     );
               });
            }

            services
               .AddOptions()
               .AddHostedService<TForgeImplementation>()
               .AddSingleton<IDataFolders, DataFolders>(serviceProvider => new DataFolders("."))
               .AddSingleton<IForgeDataFolderLock, ForgeDataFolderLock>()
               .AddSingleton<IEventBus, InMemoryEventBus>()
               .AddSingleton<ISubscriptionErrorHandler, DefaultSubscriptionErrorHandler>()
               .AddSingleton<IForgeServer, FakeForgeServer>()
               ;
         });

         this.isForgeSet = true;

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
      public ForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging) {
         this.hostBuilder.ConfigureLogging(configureLogging);
         return this;
      }

      public ForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings {

         this.AddShard<TMithrilShard>(configureDelegate);

         //register shard configuration settings
         this.hostBuilder.ConfigureServices((context, services) => {
            services.Configure<TMithrilShardSettings>(MithrilShardSettingsManager.GetSection<TMithrilShardSettings>(context.Configuration));

            //register the shard configuration setting as IMithrilShardSettings in order to allow DefaultConfigurationWriter to write default its default values
            services.AddSingleton<IMithrilShardSettings, TMithrilShardSettings>();
         });

         return this;
      }

      public ForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard {

         if (configureDelegate is null) {
            throw new ArgumentNullException(nameof(configureDelegate));
         }

         this.hostBuilder.ConfigureServices(configureDelegate);

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
      public Task RunConsoleAsync(CancellationToken cancellationToken = default) {
         //TODO: add configuration parameter to set if console logging is enabled or not and use it
         this.ConfigureLogging((context, logging) => {
            logging.AddConsole();
         });

         return this.hostBuilder.RunConsoleAsync(cancellationToken);
      }
   }
}
