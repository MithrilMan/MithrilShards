using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Forge.MithrilShards;

namespace MithrilShards.Core.Forge {
   public interface IForgeBuilder {
      ForgeBuilder AddShard<TMithrilShard>(Action<HostBuilderContext, IServiceCollection> configureDelegate) where TMithrilShard : class, IMithrilShard;

      ForgeBuilder Configure(string[] commandLineArgs, string configurationFile = "forge.config");

      ForgeBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging);

      Task RunConsoleAsync(CancellationToken cancellationToken = default);

      ForgeBuilder UseForge<TForgeImplementation>() where TForgeImplementation : class, IForge;
   }
}