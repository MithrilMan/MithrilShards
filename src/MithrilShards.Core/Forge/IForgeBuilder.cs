using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge {
   public interface IForgeBuilder {

      string ConfigurationFIleName { get; }

      /// <summary>
      /// Adds a shard into the forge.
      /// </summary>
      /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
      /// <typeparam name="TMithrilShardSettings">The type of the mithril shard settings to register to allow application configuration.</typeparam>
      /// <param name="configureDelegate">The configure delegate.</param>
      /// <returns></returns>
      IForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings;

      /// <summary>
      /// Adds a shard into the forge.
      /// </summary>
      /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
      /// <param name="configureDelegate">The configure delegate.</param>
      /// <returns></returns>
      IForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate) where TMithrilShard : class, IMithrilShard;

      IForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging);

      IForgeBuilder ExtendInnerHostBuilder(Action<IHostBuilder> extendHostBuilderAction);

      Task RunConsoleAsync(CancellationToken cancellationToken = default);

      IForgeBuilder UseForge<TForgeImplementation>(string[] commandLineArgs, string configurationFile) where TForgeImplementation : class, IForge;
   }
}