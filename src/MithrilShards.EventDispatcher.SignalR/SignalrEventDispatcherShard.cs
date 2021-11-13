using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Shards;
using MithrilShards.EventDispatcher.SignalR.Hubs;

namespace MithrilShards.EventDispatcher.SignalR;

internal class SignalrEventDispatcherShard : IMithrilShard
{
   readonly ILogger<SignalrEventDispatcherShard> _logger;
   readonly ILoggerFactory _loggerFactory;
   readonly IServiceProvider _serviceProvider;
   readonly IHostApplicationLifetime _hostApplicationLifetime;
   readonly SignalrEventDispatcherSettings _settings;
   private IWebHost? _webHost;

   public SignalrEventDispatcherShard(ILogger<SignalrEventDispatcherShard> logger, ILoggerFactory loggerFactory, IOptions<SignalrEventDispatcherSettings> options, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, IForgeBuilder forgeBuilder)
   {
      _logger = logger;
      _loggerFactory = loggerFactory;
      _serviceProvider = serviceProvider;
      _hostApplicationLifetime = hostApplicationLifetime;
      _settings = options.Value;
   }

   public ValueTask InitializeAsync(CancellationToken cancellationToken)
   {
      if (!_settings.Enabled)
      {
         _logger.LogWarning($"{nameof(SignalrEventDispatcherSettings)} disabled, SignalR Event Dispatcher will not be available");

         return default;
      }

      _webHost = new WebHostBuilder()
         .UseContentRoot(Directory.GetCurrentDirectory())
         .UseKestrel(serverOptions => { serverOptions.Listen(_settings.GetIPEndPoint()); })
         .Configure((context, app) =>
         {
            app.UseRouting()
               .UseCors("CorsPolicy")
               .UseAuthorization()
               .UseEndpoints(endpoints =>
               {
                  endpoints.MapHub<EventDispatcherHub>("/eventshub");
               })
            ;
         })
         .ConfigureServices(services =>
         {
            // register the eventbus reusing the singleton instance from the forge serviceProvider so we can intercept the events.
            services.AddSingleton<IEventBus>(_serviceProvider.GetRequiredService<IEventBus>());

            //register logs reusing configured Forge log factory (e.g. serilog logging factory)
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            services.Add(ServiceDescriptor.Singleton<ILoggerFactory>(_serviceProvider.GetRequiredService<ILoggerFactory>()));

            services.AddSignalR();

            services.AddCors(options =>
            {
               options.AddPolicy("CorsPolicy", builder =>
               {
                  builder
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .SetIsOriginAllowed(_ => true)
                     .AllowCredentials();
               });
            });

         })
         .Build();


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
