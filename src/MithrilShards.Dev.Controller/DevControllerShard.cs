using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerShard : IMithrilShard
   {
      readonly ILogger<DevControllerShard> _logger;
      readonly IServiceCollection _registeredServices;
      readonly IServiceProvider _serviceProvider;
      readonly IHostApplicationLifetime _hostApplicationLifetime;
      readonly DevAssemblyScaffolder _devAssemblyScaffolder;
      readonly DevControllerSettings _settings;

      public DevControllerShard(ILogger<DevControllerShard> logger, IOptions<DevControllerSettings> options, IServiceCollection registeredServices, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, DevAssemblyScaffolder devAssemblyScaffolder)
      {
         _logger = logger;
         _registeredServices = registeredServices;
         _serviceProvider = serviceProvider;
         _hostApplicationLifetime = hostApplicationLifetime;
         _devAssemblyScaffolder = devAssemblyScaffolder;
         _settings = options.Value;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         if (!IPEndPoint.TryParse(_settings.EndPoint, out IPEndPoint iPEndPoint))
         {
            ThrowHelper.ThrowArgumentException($"Wrong configuration parameter for {nameof(_settings.EndPoint)}");
         }

         return default;
      }

      /// <inheritdoc/>
      public ValueTask StartAsync(CancellationToken cancellationToken) => default;

      /// <inheritdoc/>
      public ValueTask StopAsync(CancellationToken cancellationToken) => default;
   }
}
