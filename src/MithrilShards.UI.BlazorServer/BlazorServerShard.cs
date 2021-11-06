using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
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
using MithrilShards.Core.Shards;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Blazorise;

namespace MithrilShards.UI.BlazorServer
{
   internal class BlazorServerShard : IMithrilShard
   {
      readonly ILogger<BlazorServerShard> _logger;
      readonly IServiceCollection _registeredServices;
      readonly IServiceProvider _serviceProvider;
      readonly IHostApplicationLifetime _hostApplicationLifetime;
      readonly BlazorServerSettings _settings;
      private IWebHost? _webHost;

      public BlazorServerShard(ILogger<BlazorServerShard> logger, IOptions<BlazorServerSettings> options, IServiceCollection registeredServices, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
      {
         _logger = logger;
         _registeredServices = registeredServices;
         _serviceProvider = serviceProvider;
         _hostApplicationLifetime = hostApplicationLifetime;
         _settings = options.Value;
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         if (!_settings.Enabled)
         {
            _logger.LogWarning($"{nameof(BlazorServerSettings)} disabled, Dev API will not be available");

            return default;
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
                     services.AddSingleton(service.ServiceType, sp =>
                     {
                        //resolve singletons from the main provider
                        var instance = _serviceProvider.GetServices(service.ServiceType).First(s => service.ImplementationType == null || s?.GetType() == service.ImplementationType);
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

               services.AddBlazorise(options => { options.ChangeTextOnKeyPress = true; })
                   .AddBootstrapProviders()
                   .AddFontAwesomeIcons();
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
                    _logger.LogInformation("BlazorServer started, listening to endpoint {ListenerLocalEndpoint}.", _settings.EndPoint);
                    await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
                 }
                 catch (OperationCanceledException)
                 {
                    // Task canceled, legit, ignoring exception.
                 }
                 catch (Exception ex)
                 {
                    _logger.LogCritical(ex, "BlazorServer exception, {BlazorServerException}. The node will still run without BlazorServer functionality.", ex.Message);
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
            _ = _webHost.StopAsync(cancellationToken);
         }

         return default;
      }
   }
}
