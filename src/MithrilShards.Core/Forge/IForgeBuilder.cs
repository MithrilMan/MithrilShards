using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Forge;

public interface IForgeBuilder
{
   /// <summary>
   /// Gets the name of the configuration file.
   /// </summary>
   string ConfigurationFileName { get; }

   /// <summary>
   /// Configures the context of the builder. Useful to access a shared Property dictionary to exchange items before a forge is built.
   /// </summary>
   /// <param name="configureDelegate">The configure delegate.</param>
   IForgeBuilder ConfigureContext(Action<HostBuilderContext> configureDelegate);

   /// <summary>
   /// Adds a shard into the forge.
   /// </summary>
   /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
   /// <typeparam name="TMithrilShardSettings">The type of the mithril shard settings to register to allow application configuration.</typeparam>
   /// <typeparam name="TMithrilShardSettingsValidator">The type of the mithril shard settings validator.</typeparam>
   /// <param name="configureDelegate">The configure delegate.</param>
   /// <param name="preBuildAction">The action to execute before the forge is built.</param>
   IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings, TMithrilShardSettingsValidator>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null)
      where TMithrilShard : class, IMithrilShard
      where TMithrilShardSettings : class, IMithrilShardSettings, new()
      where TMithrilShardSettingsValidator : class, IValidateOptions<TMithrilShardSettings>;

   /// <summary>
   /// Adds a shard into the forge.
   /// </summary>
   /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
   /// <typeparam name="TMithrilShardSettings">The type of the mithril shard settings to register to allow application configuration.</typeparam>
   /// <param name="configureDelegate">The configure delegate.</param>
   /// <param name="preBuildAction">The action to execute before the forge is built.</param>
   IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null)
      where TMithrilShard : class, IMithrilShard
      where TMithrilShardSettings : class, IMithrilShardSettings, new();

   /// <summary>
   /// Adds a shard into the forge.
   /// </summary>
   /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
   /// <param name="configureDelegate">The configure delegate.</param>
   /// <param name="preBuildAction">The action to execute before the forge is built.</param>
   IForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate, Action<IHostBuilder>? preBuildAction = null) where TMithrilShard : class, IMithrilShard;

   /// <summary>
   /// Adds a delegate for configuring the provided Microsoft.Extensions.Logging.ILoggingBuilder.
   /// This may be called multiple times.
   /// </summary>
   /// <param name="configureLogging">The delegate that configures the Microsoft.Extensions.Logging.ILoggingBuilder.</param>
   IForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging);

   /// <summary>
   /// Adds a delegate for configuring the provided Microsoft.Extensions.Hosting.IHostBuilder.
   /// </summary>
   /// <param name="extendHostBuilderAction">The delegate that configures the Microsoft.Extensions.Hosting.IHostBuilder.</param>
   IForgeBuilder ExtendInnerHostBuilder(Action<IHostBuilder> extendHostBuilderAction);

   /// <summary>
   /// Enables console support, builds and starts the host, and waits for Ctrl+C or
   /// SIGTERM to shut down.
   /// </summary>
   /// <param name="cancellationToken">A System.Threading.CancellationToken that can be used to cancel the console.</param>
   /// <returns>A System.Threading.Tasks.Task that only completes when the token is triggered or shutdown is triggered.</returns>
   Task RunConsoleAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Runs an application and block the calling thread until host shutdown.
   /// See <see cref="IApplicationLifetime.StopApplication"/> to trigger shutdown.
   /// </summary>
   /// <param name="cancellationToken"></param>
   /// <returns></returns>
   Task RunAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Uses the forge implementation.
   /// </summary>
   /// <typeparam name="TForgeImplementation">The type of the forge implementation.</typeparam>
   /// <param name="commandLineArgs">command line arguments</param>
   /// <param name="configurationFile">The configuration file.</param>
   IForgeBuilder UseForge<TForgeImplementation>(string[] commandLineArgs, string configurationFile) where TForgeImplementation : class, IForge;
}
