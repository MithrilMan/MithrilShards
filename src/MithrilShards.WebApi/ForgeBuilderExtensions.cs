using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MithrilShards.WebApi;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using System.Reflection;
using MithrilShards.Core.Shards;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using MithrilShards.WebApi.Filters.OperationFilters;
using MithrilShards.Dev.Controller;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MithrilShards.WebApi.Conventions;

namespace MithrilShards.Core.Forge;

public static class ForgeBuilderExtensions
{
   private const string SWAGGER_ROUTE_PREFIX = "docs";

   /// <summary>
   /// Uses the bitcoin chain.
   /// </summary>
   /// <param name="forgeBuilder">The forge builder.</param>
   /// <param name="options"></param>
   /// <returns></returns>
   public static IForgeBuilder UseApi(this IForgeBuilder forgeBuilder, Action<WebApiOptions>? options = null)
   {
      var optionsInstance = new WebApiOptions();
      options?.Invoke(optionsInstance);
      optionsInstance.DiscoverControllers();

      forgeBuilder.AddShard<WebApiShard, WebApiSettings>((context, services) =>
      {
         if (optionsInstance.EnablePublicApi)
         {
            services.AddApiServiceDefinition(new ApiServiceDefinition
            {
               Enabled = context.GetShardSettings<WebApiSettings>()!.Enabled,
               Area = WebApiArea.AREA_API,
               Name = "API",
               Description = optionsInstance.PublicApiDescription,
               Version = "v1",
            });
         }
      }, preBuildAction: (hostBuilder) =>
      {

         hostBuilder.ConfigureServices((context, services) =>
         {
            var tempServiceProvider = services.BuildServiceProvider();
            var logger = tempServiceProvider.GetService<ILogger<WebApiShard>>();

            if (!context.GetShardSettings<WebApiSettings>()!.Enabled)
            {
               logger?.LogDebug("Web API services disabled, skipping Initialization.");
               return;
            }

            IEnumerable<Assembly> assembliesToInspect = tempServiceProvider.GetService<IEnumerable<IMithrilShard>>()!
            .Select(shard => shard.GetType().Assembly)
            .Concat(optionsInstance.Seeker.GetAssemblies());

            IEnumerable<ApiServiceDefinition> apiServiceDefinitions = tempServiceProvider.GetService<IEnumerable<ApiServiceDefinition>>()!;

            services
               .AddRouting()
               .AddSwaggerGen(setup =>
               {
                  logger?.LogDebug("Configuring WEB API services.");

                  setup.OperationFilter<AuthResponsesOperationFilter>();

                  var documentFilterMethod = setup.GetType().GetMethod("DocumentFilter");
                  foreach (var apiServiceDefinition in apiServiceDefinitions)
                  {
                     setup.SwaggerDoc(
                        $"{apiServiceDefinition.Area}-{apiServiceDefinition.Version}",
                        new OpenApiInfo
                        {
                           Title = apiServiceDefinition.Name,
                           Version = $"{apiServiceDefinition.Area}-{apiServiceDefinition.Version}",
                           Description = apiServiceDefinition.Description
                        });

                     setup.DocInclusionPredicate((x, api) =>
                     {
                           // actually version number isn't considered
                           var parts = x.Split("-");
                        return parts[0] == api.GroupName;
                     });

                     foreach (IDocumentFilter documentFilter in apiServiceDefinition.DocumentFilters)
                     {
                        documentFilterMethod!.MakeGenericMethod(documentFilter.GetType()).Invoke(setup, null);
                           //apiServiceDefinition.SwaggerGenConfiguration?.Invoke(setup);
                           logger?.LogDebug("Added document filter type {DocumentFilterType}.", documentFilter.GetType());
                     }
                  }

                     /// Adds XML documentation to swagger in order to produce a better documentation on swagger UI.
                     /// Can work only if the assembly has been compiled with the option to generate the XML documentation file
                     /// and the xml file name is the same as the assembly name (except the extension).
                     foreach (Assembly assembly in assembliesToInspect)
                  {
                     var xmlFile = $"{assembly.GetName().Name}.xml";
                     var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                     if (File.Exists(xmlPath))
                     {
                        setup.IncludeXmlComments(xmlPath);
                     }
                     else
                     {
                        logger?.LogTrace("Cannot find API documentation file {ApiDocumentationPath}", xmlPath);
                     }
                  }
               });

            var mvcBuilder = services
               .AddControllers(configure =>
               {
                     // adding filters to consume and produce json and to disable controllers that don't belong to an Area
                     configure.Filters.Add(new ConsumesAttribute("application/json"));
                  configure.Filters.Add(new ProducesAttribute("application/json"));
                  configure.Filters.Add<DisableByEndPointActionFilterAttribute>();

                  configure.Conventions.Add(new ApiExplorerGroupPerVersionConvention());
               })
               .AddJsonOptions(options =>
               {
                  options.JsonSerializerOptions.WriteIndented = true;
                  options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                  options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
               });

               // creates application part for each shard and assembly containing controllers.
               foreach (Assembly assembly in assembliesToInspect)
            {
               mvcBuilder.AddApplicationPart(assembly);
            }

         });

         hostBuilder.ConfigureWebHost(configure =>
         {
            configure
               .UseKestrel(serverOptions =>
               {
                  var logger = serverOptions.ApplicationServices.GetService<ILogger<WebApiShard>>();

                  IEnumerable<ApiServiceDefinition> apiServiceDefinitions = serverOptions.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>()!;
                  var webApiSettings = serverOptions.ApplicationServices.GetService<IOptions<WebApiSettings>>()!.Value;

                     // sanity check of registered ApiServiceDefinition
                     foreach (var apiServiceDefinition in apiServiceDefinitions)
                  {
                     apiServiceDefinition.CheckValidity();
                  }

                     //check for duplicated areas
                     var duplicatedArea = apiServiceDefinitions.GroupBy(d => d.Area).FirstOrDefault(g => g.Count() > 1);
                  if (duplicatedArea != null)
                  {
                     ThrowHelper.ThrowArgumentException($"Multiple {nameof(ApiServiceDefinition)} defined with the same Area {duplicatedArea.Key}");
                  }

                  if (webApiSettings.Enabled)
                  {
                     serverOptions.Listen(webApiSettings.GetIPEndPoint(), options =>
                      {
                         if (webApiSettings.Https)
                         {
                            options.UseHttps();
                         }
                      });

                     string rootUrl = webApiSettings.GetListeningUrl();
                     string apiUrl = $"{rootUrl}/{SWAGGER_ROUTE_PREFIX}";
                     logger?.LogInformation("Configured WEB API listener to {ApiEndPoint}. Swagger documentation at URL {SwaggerEndPoint}", rootUrl, apiUrl);
                  }
               })
               .Configure(app => SwaggerConfiguration(app, optionsInstance));
         });
      });

      return forgeBuilder;
   }

   private static void SwaggerConfiguration(IApplicationBuilder app, WebApiOptions options)
   {
      var logger = app.ApplicationServices.GetService<ILogger<WebApiShard>>()!;
      var webApiSettings = app.ApplicationServices.GetService<IOptions<WebApiSettings>>()!.Value;

      if (!webApiSettings.Enabled)
      {
         logger.LogDebug("Web API services disabled, skipping SwaggerConfiguration.");
         return;
      }

      IEnumerable<ApiServiceDefinition> apiServiceDefinitions = app.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>()!;

      app
         .UseRouting()
         .UseSwagger(setup =>
         {
            setup.RouteTemplate = "docs/{documentName}/openapi.json";
         })
         .UseEndpoints(endpoints =>
         {
            endpoints.MapControllers();
         })
         .UseSwaggerUI(setup =>
         {
            setup.RoutePrefix = SWAGGER_ROUTE_PREFIX;
            setup.InjectStylesheet("/swagger_ui/custom.css");
            setup.DocumentTitle = options.Title;
            setup.EnableFilter();

            setup.IndexStream = () => typeof(WebApiShard).Assembly.GetManifestResourceStream("MithrilShards.WebApi.Resources.swagger_ui.index.html");

            logger.LogDebug("Configuring WEB API OpenAPI documents.");

            foreach (var apiServiceDefinition in apiServiceDefinitions)
            {
               if (apiServiceDefinition.Enabled)
               {
                  string swaggerEndPointUrl = $"/docs/{apiServiceDefinition.Area}-{apiServiceDefinition.Version}/openapi.json";
                  setup.SwaggerEndpoint(swaggerEndPointUrl, apiServiceDefinition.Name);

                  var swaggerUIRoot = $"{webApiSettings.GetListeningUrl()}{swaggerEndPointUrl}";
                  logger.LogInformation("{WebApiService} OpenApi service specification generated at {SwaggerEndPoint}", apiServiceDefinition.Name, swaggerUIRoot);
               }
               else
               {
                  logger.LogWarning("WEB API service {WebApiService} disabled, area {WebApiServiceArea} will not be available.", apiServiceDefinition.Name, apiServiceDefinition.Area);
               }
            }
         })
         .UseStaticFiles(new StaticFileOptions
         {
            RequestPath = new PathString($"/swagger_ui"),
            FileProvider = new EmbeddedFileProvider(typeof(WebApiShard).Assembly, "MithrilShards.WebApi.Resources.swagger_ui")
         });
   }
}
