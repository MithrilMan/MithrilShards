using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Shards;
using MithrilShards.Core.Shards.Validation;

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
      private List<Action<IHostBuilder>> _preBuildActions = new List<Action<IHostBuilder>>();
      private readonly HostBuilder _hostBuilder;

      public string ConfigurationFileName { get; private set; } = null!; //set to something meaningful during initialization

      public ForgeBuilder()
      {
         // create a temporary logger that logs on console to communicate pre-initialization errors that may happens for example with missing configuration files
         ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Warning).AddConsole());
         _logger = loggerFactory.CreateLogger<ForgeBuilder>();

         _hostBuilder = new HostBuilder();

         // Add a new service provider configuration
         _hostBuilder
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
         if (fileContext.Exception is FileNotFoundException)
         {
            _createDefaultConfigurationFileNeeded = true;

            _logger.LogWarning($"Missing configuration file {ConfigurationFileName}, creating one with default values.");

            //default file created, no need to throw error
            fileContext.Ignore = true;
         }
         else
         {
            throw new ForgeBuilderException("Invalid settings file", fileContext.Exception);
         }
      }

      public IForgeBuilder UseForge<TForgeImplementation>(string[] commandLineArgs, string configurationFile = "forge-settings.json") where TForgeImplementation : class, IForge
      {
         if (_isForgeSet)
         {
            throw new Exception($"Forge already set. Only one call to {nameof(UseForge)} is allowed");
         }

         _ = _hostBuilder.ConfigureServices((context, services) =>
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
               .AddHostedService<ValidationHostedService>() // used to validate IOptions at startup, when they use ValidateOnStart (shards are automatically configured to validate asap)
               .AddSingleton<IServiceCollection>(services) // register forge service collection in order to create other sandboxed serviceProviders in other Hosts (e.g. for API purpose)
               .AddSingleton<IForge, TForgeImplementation>()
               .AddHostedService<TForgeImplementation>(serviceProvider => (TForgeImplementation)serviceProvider.GetRequiredService<IForge>())
               .ConfigureForge(context);
         });

         _isForgeSet = true;

         Configure(commandLineArgs, configurationFile);

         return this;
      }


      /// <inheritdoc/>
      public IForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging)
      {
         _hostBuilder.ConfigureLogging(configureLogging);
         return this;
      }

      /// <inheritdoc/>
      public IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings, new()
      {
         AddShard<TMithrilShard, TMithrilShardSettings, DataAnnotationValidateOptions<TMithrilShardSettings>>(configureDelegate, preBuildAction);

         return this;
      }

      public IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings, TMithrilShardSettingsValidator>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings, new()
         where TMithrilShardSettingsValidator : class, IValidateOptions<TMithrilShardSettings>
      {

         //register shard configuration settings
         _hostBuilder.ConfigureServices((context, services) =>
         {
            var optionsBuilder = services
                .AddOptions<TMithrilShardSettings>()
                .Bind(MithrilShardSettingsManager.GetSection<TMithrilShardSettings>(context.Configuration))
                .ValidateOnStart();

            optionsBuilder.Services.AddSingleton<IValidateOptions<TMithrilShardSettings>>(new DataAnnotationValidateOptions<TMithrilShardSettings>(optionsBuilder.Name));

            //register the shard configuration setting as IMithrilShardSettings in order to allow DefaultConfigurationWriter to write default its default values
            services.AddSingleton<IMithrilShardSettings, TMithrilShardSettings>();

            context.SetShardSettings<TMithrilShardSettings>(services);
         });

         AddShard<TMithrilShard>(configureDelegate, preBuildAction);

         return this;
      }



      /// <inheritdoc/>
      public IForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null)
         where TMithrilShard : class, IMithrilShard
      {
         _ = configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate));

         _hostBuilder.ConfigureServices((context, services) =>
         {
            services.AddSingleton<IMithrilShard, TMithrilShard>();
            configureDelegate(context, services);
         });

         if (preBuildAction != null)
         {
            _preBuildActions.Add(preBuildAction);
         }

         return this;
      }

      /// <inheritdoc/>
      public IForgeBuilder ExtendInnerHostBuilder(Action<IHostBuilder> extendHostBuilderAction)
      {
         extendHostBuilderAction(_hostBuilder);
         return this;
      }


      /// <inheritdoc/>
      public Task RunConsoleAsync(CancellationToken cancellationToken = default)
      {
         EnsureForgeIsSet();

         foreach (var preBuildAction in _preBuildActions)
         {
            preBuildAction.Invoke(_hostBuilder);
         }

         try
         {
            return _hostBuilder.RunConsoleAsync(cancellationToken);
         }
         catch (OptionsValidationException ex)
         {
            _logger.LogError("Cannot run the forge because of configuration errors.");

            foreach (var validationFailure in ex.Failures)
            {
               _logger.LogError("Configuration problem in '{MithrilShardSettings}': {WrongSetting}", ex.OptionsType.Name, validationFailure);
            }

            return Task.CompletedTask;
         }
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

         _ = _hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
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

      /// <inheritdoc/>
      public IForgeBuilder ConfigureContext(Action<HostBuilderContext> configureContextDelegate)
      {
         _hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) => configureContextDelegate(context));
         return this;
      }
   }
}
