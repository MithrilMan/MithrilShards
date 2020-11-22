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
      readonly ILogger<DevControllerShard> logger;
      readonly IServiceCollection registeredServices;
      readonly IServiceProvider serviceProvider;
      readonly IHostApplicationLifetime hostApplicationLifetime;
      readonly DevAssemblyScaffolder devAssemblyScaffolder;
      readonly DevControllerSettings settings;
      private IWebHost? webHost;

      public DevControllerShard(ILogger<DevControllerShard> logger, IOptions<DevControllerSettings> options, IServiceCollection registeredServices, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, DevAssemblyScaffolder devAssemblyScaffolder)
      {
         this.logger = logger;
         this.registeredServices = registeredServices;
         this.serviceProvider = serviceProvider;
         this.hostApplicationLifetime = hostApplicationLifetime;
         this.devAssemblyScaffolder = devAssemblyScaffolder;
         this.settings = options.Value;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         if (!settings.Enabled)
         {
            this.logger.LogWarning($"{nameof(DevControllerSettings)} disabled, Dev API will not be available");

            return default;
         }

         if (!IPEndPoint.TryParse(settings.EndPoint, out IPEndPoint iPEndPoint))
         {
            ThrowHelper.ThrowArgumentException($"Wrong configuration parameter for {nameof(settings.EndPoint)}");
         }


         this.webHost = new WebHostBuilder()
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
               foreach (ServiceDescriptor service in registeredServices)
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
                     services.AddSingleton(service.ServiceType, sp => serviceProvider.GetService(service.ServiceType)); //resolve singletons from the main provider
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

               IEnumerable<Assembly> assembliesToScaffold = this.serviceProvider.GetService<IEnumerable<IMithrilShard>>()
                  .Select(shard => shard.GetType().Assembly)
                  .Concat(this.devAssemblyScaffolder?.GetAssemblies());

               foreach (Assembly shardAssembly in assembliesToScaffold)
               {
                  mvcBuilder.AddApplicationPart(shardAssembly);
               }
            }).Build();


         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         if (webHost != null)
         {
            _ = Task.Run(async () =>
              {
                 try
                 {
                    this.logger.LogInformation("DevController API started, listening to endpoint {ListenerLocalEndpoint}/swagger.", settings.EndPoint);
                    await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
                 }
                 catch (OperationCanceledException)
                 {
                    // Task canceled, legit, ignoring exception.
                 }
                 catch (Exception ex)
                 {
                    this.logger.LogCritical(ex, "DevController API exception, {DevControllerException}. App will still run, without DevController funtionality", ex.Message);
                    // if we want to stop the application in case of exception, uncomment line below
                    // hostApplicationLifetime.StopApplication();
                 }
              });
         }


         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         if (webHost != null)
         {
            webHost.StopAsync(cancellationToken);
         }

         return default;
      }
   }
}
