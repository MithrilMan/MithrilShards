using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.MithrilShards;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerAddressBook;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Core.Forge {
   public class ForgeBuilder : IForgeBuilder {
      const string CONFIGURATION_FILE = "forge-settings.json";

      public readonly HostBuilder hostBuilder;
      private bool isForgeSet = false;
      private bool createDefaultConfigurationFileNeeded = false;
      public string ConfigurationFileName { get; private set; }

      public ForgeBuilder() {
         this.hostBuilder = new HostBuilder();

         // Add a new service provider configuration
         this.hostBuilder.UseDefaultServiceProvider((context, options) => {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            options.ValidateOnBuild = true;
         });
      }

      /// <summary>
      /// Creates the default configuration file if it's missing.
      /// </summary>
      /// <returns></returns>
      private void CreateDefaultConfigurationFile(HostBuilderContext hostingContext, FileLoadExceptionContext context) {
         this.createDefaultConfigurationFileNeeded = true;
         this.ConfigurationFileName = context.Provider.Source.Path;

         //default file created, no need to throw error
         context.Ignore = true;
      }

      public IForgeBuilder UseForge<TForgeImplementation>(string[] commandLineArgs, string configurationFile = "forge-settings.json") where TForgeImplementation : class, IForge {
         if (this.isForgeSet) {
            throw new Exception($"Forge already set. Only one call to {nameof(UseForge)} is allowed");
         }

         _ = this.hostBuilder.ConfigureServices((context, services) => {

            if (this.createDefaultConfigurationFileNeeded) {
               services.AddSingleton<DefaultConfigurationWriter>(services => {
                  return new DefaultConfigurationWriter(
                     services.GetService<ILoggerFactory>().CreateLogger<DefaultConfigurationWriter>(),
                     services.GetServices<IMithrilShardSettings>(),
                     this.ConfigurationFileName
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
               .AddSingleton<IForgeConnectivity, FakeForgeConnectivity>()
               .AddSingleton<IInitialBlockDownloadState, InitialBlockDownloadState>()
               .AddSingleton<IDateTimeProvider, DateTimeProvider>()
               .AddSingleton<INetworkMessageSerializerManager, NetworkMessageSerializerManager>()
               .AddSingleton<INetworkMessageProcessorFactory, NetworkMessageProcessorFactory>()
               .AddSingleton<IRandomNumberGenerator, DefaultRandomNumberGenerator>()
               .AddSingleton<IPeerContextFactory, PeerContextFactory<PeerContext>>()
               .AddHostedService<ConnectionManager>()

               //add peer address book with peer score manager
               .AddSingleton<PeerAddressBook>() //required to forward the instance to 2 interfaces below
               .AddSingleton<IPeerAddressBook>(serviceProvider => serviceProvider.GetRequiredService<PeerAddressBook>())
               .AddSingleton<IPeerScoreManager>(serviceProvider => serviceProvider.GetRequiredService<PeerAddressBook>())
               ;
         });

         this.isForgeSet = true;

         this.Configure(commandLineArgs, configurationFile);

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
      public IForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging) {
         this.hostBuilder.ConfigureLogging(configureLogging);
         return this;
      }

      public IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
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

      public IForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard {

         if (configureDelegate is null) {
            throw new ArgumentNullException(nameof(configureDelegate));
         }

         this.hostBuilder.ConfigureServices((context, services) => {
            services.AddSingleton<IMithrilShard, TMithrilShard>();
            configureDelegate(context, services);
         });

         return this;
      }

      public IForgeBuilder ExtendInnerHostBuilder(Action<IHostBuilder> extendHostBuilderAction) {
         extendHostBuilderAction(this.hostBuilder);
         return this;
      }

      /// <summary>
      /// Adds the console log reading settings from Logging section if configuration file and displaying on standard console
      /// </summary>
      /// <returns></returns>
      public IForgeBuilder AddConsoleLog() {
         this.ConfigureLogging((context, logging) => {
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
      public Task RunConsoleAsync(CancellationToken cancellationToken = default) {
         this.EnsureForgeIsSet();

         return this.hostBuilder.RunConsoleAsync(cancellationToken);
      }

      private void EnsureForgeIsSet() {
         if (!this.isForgeSet) {
            throw new ForgeBuilderException("Forge must be set. A call to UseForge is required");
         }
      }

      private IForgeBuilder Configure(string[] commandLineArgs, string configurationFile = CONFIGURATION_FILE) {
         this.ConfigurationFileName = configurationFile ?? CONFIGURATION_FILE;

         _ = this.hostBuilder.ConfigureAppConfiguration((hostingContext, config) => {

            config.AddJsonFile(this.ConfigurationFileName, optional: false, reloadOnChange: true);

            config.AddEnvironmentVariables("FORGE_");

            if (commandLineArgs != null) {
               config.AddCommandLine(commandLineArgs);
            }

            config.SetFileLoadExceptionHandler(context => this.CreateDefaultConfigurationFile(hostingContext, context));
         });

         return this;
      }
   }
}
