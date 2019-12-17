using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge {
   public interface IForgeBuilder {

      /// <summary>
      /// Adds a shard into the forge.
      /// </summary>
      /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
      /// <typeparam name="TMithrilShardSettings">The type of the mithril shard settings to register to allow application configuration.</typeparam>
      /// <param name="configureDelegate">The configure delegate.</param>
      /// <returns></returns>
      ForgeBuilder AddShard<TMithrilShard, TMithrilShardSettings>(Action<HostBuilderContext, IServiceCollection> configureDelegate)
         where TMithrilShard : class, IMithrilShard
         where TMithrilShardSettings : class, IMithrilShardSettings;

      /// <summary>
      /// Adds a shard into the forge.
      /// </summary>
      /// <typeparam name="TMithrilShard">The type of the mithril shard.</typeparam>
      /// <param name="configureDelegate">The configure delegate.</param>
      /// <returns></returns>
      ForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate) where TMithrilShard : class, IMithrilShard;
    
      ForgeBuilder Configure(string[] commandLineArgs, string configurationFile = "forge.config");

      ForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging);

      Task RunConsoleAsync(CancellationToken cancellationToken = default);

      ForgeBuilder UseForge<TForgeImplementation>() where TForgeImplementation : class, IForge;
   }
}