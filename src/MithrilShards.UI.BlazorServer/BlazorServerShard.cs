using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.Shards;

namespace MithrilShards.UI.BlazorServer;

internal class BlazorServerShard(
   ILogger<BlazorServerShard> logger,
   IOptions<BlazorServerSettings> options,
   IServiceCollection registeredServices,
   IServiceProvider serviceProvider
   ) : IMithrilShard
{
   readonly BlazorServerSettings _settings = options.Value;
   private IWebHost? _webHost;

   public Task InitializeAsync(CancellationToken cancellationToken)
   {
      if (!_settings.Enabled)
      {
         logger.LogWarning($"{nameof(BlazorServerSettings)} disabled, Dev API will not be available");

         return Task.CompletedTask;
      }

      _webHost = new WebHostBuilder()
         .UseContentRoot(Directory.GetCurrentDirectory())
         .UseKestrel(serverOptions => { serverOptions.Listen(_settings.GetIPEndPoint()); })
         .Configure((context, app) =>
         {
            if (context.HostingEnvironment.IsDevelopment())
            {
               app.UseDeveloperExceptionPage();
            }
            else
            {
               app.UseExceptionHandler("/Error");
               // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
               app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
               endpoints.MapBlazorHub();
               endpoints.MapFallbackToPage("/_Host");
            });
         })
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
                  services.AddSingleton(service.ServiceType, sp =>
                  {
                     //resolve singletons from the main provider
                     var instance = serviceProvider.GetServices(service.ServiceType).First(s => service.ImplementationType == null || s?.GetType() == service.ImplementationType);
                     if (instance == null) ThrowHelper.ThrowNullReferenceException($"Service type {service.ServiceType.Name} not found.");

                     return instance;
                  });
               }
               else
               {
                  services.Add(service);
               }
            }

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSignalR(e =>
            {
               e.MaximumReceiveMessageSize = 102400000;

            });

            services.AddBlazorise(options => { /*options.ChangeTextOnKeyPress = true;*/ })
                .AddBootstrapProviders()
                .AddFontAwesomeIcons();
         }).Build();


      return Task.CompletedTask;
   }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      if (_webHost == null)
      {
         logger.LogWarning("BlazorServer not initialized, skipping start.");
         return Task.CompletedTask;
      }

      _ = Task.Run(async () =>
        {
           try
           {
              logger.LogInformation("BlazorServer started, listening to endpoint {ListenerLocalEndpoint}.", _settings.EndPoint);
              await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
           }
           catch (OperationCanceledException)
           {
              // Task canceled, legit, ignoring exception.
           }
           catch (Exception ex)
           {
              logger.LogCritical(ex, "BlazorServer exception, {BlazorServerException}. The node will still run without BlazorServer functionality.", ex.Message);
              // if we want to stop the application in case of exception, uncomment line below
              // hostApplicationLifetime.StopApplication();
           }
        }, cancellationToken);


      return Task.CompletedTask;
   }

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      if (_webHost == null) return;

      await _webHost.StopAsync(cancellationToken).ConfigureAwait(false);
   }
}
