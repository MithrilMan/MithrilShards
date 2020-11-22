using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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
      private IWebHost? _webHost;

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
         if (!_settings.Enabled)
         {
            _logger.LogWarning($"{nameof(DevControllerSettings)} disabled, Dev API will not be available");

            return default;
         }

         if (!IPEndPoint.TryParse(_settings.EndPoint, out IPEndPoint iPEndPoint))
         {
            ThrowHelper.ThrowArgumentException($"Wrong configuration parameter for {nameof(_settings.EndPoint)}");
         }


         _webHost = new WebHostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseKestrel(serverOptions => { serverOptions.Listen(iPEndPoint); })
            .Configure(app => app
               .UseRouting()
               .UseSwagger()
               .UseSwaggerUI(setup => setup.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev Controller"))
               .UseEndpoints(endpoints => endpoints.MapControllers())
               )
            .ConfigureServices(services =>
            {
               // copies all the services registered in the forge, maintaining eventual singleton instances
               // also copies over singleton instances already defined
               foreach (ServiceDescriptor service in _registeredServices)
               {
                  if (service.ServiceType == typeof(IHostedService))
                  {
                     //prevent to start again an hosted service that's already running
                     continue;
                  }
                  // open types can't be singletons
                  else if (service.ServiceType.IsGenericType || service.Lifetime == ServiceLifetime.Scoped)
                  {
                     services.Add(service);
                  }
                  else if (service.Lifetime == ServiceLifetime.Singleton)
                  {
                     services.AddSingleton(service.ServiceType, sp => _serviceProvider.GetService(service.ServiceType)); //resolve singletons from the main provider
                  }
                  else
                  {
                     services.Add(service);
                  }
               }

               services
                  .AddSwaggerGen(setup => setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Dev Controller", Version = "v1" }));


               IMvcBuilder mvcBuilder = services.AddControllers()
                  .ConfigureApplicationPartManager(mgr =>
                  {
                     mgr.FeatureProviders.Clear();
                     mgr.FeatureProviders.Add(new DevControllerFeatureProvider());
                  })
                  .AddMvcOptions(options => options.Conventions.Add(new DevControllerConvetion()));

               IEnumerable<Assembly> assembliesToScaffold = _serviceProvider.GetService<IEnumerable<IMithrilShard>>()
                  .Select(shard => shard.GetType().Assembly)
                  .Concat(_devAssemblyScaffolder?.GetAssemblies());

               foreach (Assembly shardAssembly in assembliesToScaffold)
               {
                  mvcBuilder.AddApplicationPart(shardAssembly);
               }
            }).Build();


         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         if (_webHost != null)
         {
            _ = Task.Run(async () =>
              {
                 try
                 {
                    _logger.LogInformation("DevController API started, listening to endpoint {ListenerLocalEndpoint}/swagger.", _settings.EndPoint);
                    await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
                 }
                 catch (OperationCanceledException)
                 {
                    // Task canceled, legit, ignoring exception.
                 }
                 catch (Exception ex)
                 {
                    _logger.LogCritical(ex, "DevController API exception, {DevControllerException}. App will still run, without DevController funtionality", ex.Message);
                    // if we want to stop the application in case of exception, uncomment line below
                    // hostApplicationLifetime.StopApplication();
                 }
              });
         }


         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         if (_webHost != null)
         {
            _webHost.StopAsync(cancellationToken);
         }

         return default;
      }
   }
}
