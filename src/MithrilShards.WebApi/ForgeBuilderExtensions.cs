using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MithrilShards.WebApi;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using System.Reflection;
using MithrilShards.Core.MithrilShards;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using MithrilShards.WebApi.Filters.OperationFilters;
using MithrilShards.Dev.Controller;

namespace MithrilShards.Core.Forge
{
   public static class ForgeBuilderExtensions
   {
      private const string SWAGGER_ROUTE_PREFIX = "api";

      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <returns></returns>
      public static IForgeBuilder AddApiService(this IForgeBuilder forgeBuilder, ApiServiceDefinition apiServiceDefinition)
      {
         if (forgeBuilder is null) ThrowHelper.ThrowArgumentException(nameof(forgeBuilder));

         forgeBuilder.ConfigureContext(context =>
         {
            string key = nameof(ApiServiceDefinition);

            List<ApiServiceDefinition> apiServiceDefinitions;

            if (context.Properties.TryGetValue(key, out object? item))
            {
               apiServiceDefinitions = item as List<ApiServiceDefinition> ?? new List<ApiServiceDefinition>();
            }
            else
            {
               apiServiceDefinitions = new List<ApiServiceDefinition>();
            }

            apiServiceDefinitions.Add(apiServiceDefinition);
            context.Properties[key] = apiServiceDefinitions;
         });

         return forgeBuilder;
      }

      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <returns></returns>
      public static IForgeBuilder UseApi(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<WebApiShard>((context, services) =>
         {


         }, preBuildAction: (iHostBuilder) =>
         {
            iHostBuilder.ConfigureWebHost(configure =>
            {
               configure
                  .UseKestrel(serverOptions =>
                  {
                     IEnumerable<ApiServiceDefinition> apiServiceDefinitions = serverOptions.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>();

                     foreach (var apiServiceDefinition in apiServiceDefinitions)
                     {
                        if (!apiServiceDefinition.Enabled) continue; //don't listen disabled api services

                        serverOptions.Listen(apiServiceDefinition.EndPoint, options =>
                         {
                            if (apiServiceDefinition.Https)
                            {
                               options.UseHttps();
                            }
                         });
                     }
                  })
                  .ConfigureServices(services =>
                  {
                     var tempServiceProvider = services.BuildServiceProvider();
                     var logger = tempServiceProvider.GetService<ILogger<WebApiShard>>();

                     IEnumerable<Assembly> assembliesToScaffold = tempServiceProvider.GetService<IEnumerable<IMithrilShard>>()
                     .Select(shard => shard.GetType().Assembly);
                     // .Concat(_devAssemblyScaffolder?.GetAssemblies());

                     IEnumerable<ApiServiceDefinition> apiServiceDefinitions = tempServiceProvider.GetService<IEnumerable<ApiServiceDefinition>>();

                     services
                        .AddRouting()
                        .AddSwaggerGen(setup =>
                        {
                           logger.LogDebug("Configuring WEB API services.");

                           setup.OperationFilter<AuthResponsesOperationFilter>();

                           var documentFilterMethod = setup.GetType().GetMethod("DocumentFilter");
                           foreach (var apiServiceDefinition in apiServiceDefinitions)
                           {
                              setup.SwaggerDoc($"{apiServiceDefinition.Area}-{apiServiceDefinition.Version}", new OpenApiInfo { Title = apiServiceDefinition.Name, Version = apiServiceDefinition.Version });
                              foreach (IDocumentFilter documentFilter in apiServiceDefinition.DocumentFilters)
                              {
                                 documentFilterMethod!.MakeGenericMethod(documentFilter.GetType()).Invoke(setup, null);
                                 //apiServiceDefinition.SwaggerGenConfiguration?.Invoke(setup);
                                 logger.LogDebug("Added document filter type {DocumentFilterType}.", documentFilter.GetType());
                              }
                           }

                           /// Adds XML documentation to swagger in order to produce a better documentation on swagger UI.
                           /// Can work only if the assembly has been compiled with the option to generate the XML documentation file
                           /// and the xml file name is the same as the assembly name (except the extension).
                           foreach (Assembly shardAssembly in assembliesToScaffold)
                           {
                              var xmlFile = $"{shardAssembly.GetName().Name}.xml";
                              var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                              if (File.Exists(xmlPath))
                              {
                                 setup.IncludeXmlComments(xmlPath);
                              }
                              else
                              {
                                 logger.LogDebug("Cannot find API documentation file {ApiDocumentationPath}", xmlPath);
                              }
                           }
                        });

                     var mvcBuilder = services
                        .AddControllers(configure => configure.Filters.Add<DisableByEndPointActionFilterAttribute>())
                        .AddJsonOptions(options =>
                        {
                           options.JsonSerializerOptions.WriteIndented = true;
                           options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        });

                     //   .ConfigureApplicationPartManager(mgr =>
                     //    {
                     //       mgr.FeatureProviders.Clear();
                     //       mgr.FeatureProviders.Add(new DevControllerFeatureProvider());
                     //    })
                     //.AddMvcOptions(options => options.Conventions.Add(new DevControllerConvetion()));



                     // creates application part for each shard and assembly containing controllers.
                     foreach (Assembly shardAssembly in assembliesToScaffold)
                     {
                        mvcBuilder.AddApplicationPart(shardAssembly);
                     }
                  })
                  .Configure(app => app
                     .UseRouting()
                     .UseSwagger()
                     .UseSwaggerUI(setup =>
                     {
                        setup.RoutePrefix = SWAGGER_ROUTE_PREFIX;
                        setup.InjectStylesheet("/swagger_ui/custom.css");

                        IEnumerable<ApiServiceDefinition> apiServiceDefinitions = app.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>();
                        var logger = app.ApplicationServices.GetService<ILogger<WebApiShard>>();

                        logger.LogDebug("Configuring WEB API EndPoints.");

                        foreach (var apiServiceDefinition in apiServiceDefinitions)
                        {
                           if (apiServiceDefinition.Enabled)
                           {
                              string swaggerEndPointUrl = $"/swagger/{apiServiceDefinition.Area}-{apiServiceDefinition.Version}/swagger.json";
                              setup.SwaggerEndpoint(swaggerEndPointUrl, apiServiceDefinition.Name);

                              string rootUrl = $"{(apiServiceDefinition.Https ? "https" : "http")}://{apiServiceDefinition.EndPoint}";
                              string apiUrl = $"{rootUrl}/{SWAGGER_ROUTE_PREFIX}";
                              logger.LogInformation("Configured API listener to {ApiEndPoint}. Swagger endpoint at URL {SwaggerEndPoint}", apiUrl, rootUrl + swaggerEndPointUrl);
                           }
                           else
                           {
                              logger.LogWarning("WEB API service {WebApiService} disabled, {WebApiServiceEndPoint} will not be available.", apiServiceDefinition.Name, apiServiceDefinition.EndPoint);
                           }
                        }

                        app.UseStaticFiles(new StaticFileOptions
                        {
                           RequestPath = new PathString($"/swagger_ui"),
                           FileProvider = new EmbeddedFileProvider(typeof(WebApiShard).Assembly, "MithrilShards.WebApi.Resources.swagger_ui")
                        })
                        .UseEndpoints(endpoints =>
                        {
                           endpoints.MapControllers();
                        });


                     })
                  );
            });
         });

         return forgeBuilder;
      }
   }
}