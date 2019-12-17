using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge.MithrilShards;

namespace MithrilShards.Core.Forge {
   public class ForgeBuilder : IForgeBuilder {
      const string CONFIGURATION_FILE = "forge.config.json";

      private readonly HostBuilder hostBuilder;
      private bool isForgeSet = false;

      public ForgeBuilder() {
         this.hostBuilder = new HostBuilder();
      }


      public ForgeBuilder Configure(string[] commandLineArgs, string configurationFile = CONFIGURATION_FILE) {
         _ = this.hostBuilder.ConfigureAppConfiguration((hostingContext, config) => {

            configurationFile ??= CONFIGURATION_FILE;

            config.AddJsonFile(configurationFile, optional: false);
            config.AddEnvironmentVariables();

            if (commandLineArgs != null) {
               config.AddCommandLine(commandLineArgs);
            }
         });

         return this;
      }

      public ForgeBuilder UseForge<TForgeImplementation>() where TForgeImplementation : class, IForge {
         if (this.isForgeSet) {
            throw new Exception($"Forge already set. Only one call to {nameof(UseForge)} is allowed");
         }

         _ = this.hostBuilder.ConfigureServices(services => {

            services
               .AddOptions()
               .AddHostedService<TForgeImplementation>()
               .AddSingleton<IDataFolders, DataFolders>(serviceProvider => new DataFolders("."))
               .AddSingleton<IForgeLifetime, ForgeLifetime>()
               .AddSingleton<IForgeDataFolderLock, ForgeDataFolderLock>()
               .AddSingleton<IEventBus, InMemoryEventBus>()
               .AddSingleton<ISubscriptionErrorHandler, DefaultSubscriptionErrorHandler>()
               .AddSingleton<IForgeServer, FakeForgeServer>()

               .AddSingleton<ICoreServices, CoreServices>()
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

      public ForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate) where TMithrilShard : class, IMithrilShard {
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
