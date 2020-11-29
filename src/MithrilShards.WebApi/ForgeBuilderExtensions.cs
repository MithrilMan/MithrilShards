using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MithrilShards.Core.Forge;
using MithrilShards.WebApi;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using System.Reflection;
using MithrilShards.Core.MithrilShards;
using System.Linq;

namespace MithrilShards.Core.Forge
{
   public static class ForgeBuilderExtensions
   {

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
            //string key = nameof(ApiServiceDefinition);
            //List<ApiServiceDefinition> apiServiceDefinitions;
            //if (iHostBuilder.Properties.TryGetValue(key, out object? item) && item is List<ApiServiceDefinition>)
            //{
            //   apiServiceDefinitions = (List<ApiServiceDefinition>)item;
            //}
            //else
            //{
            //   apiServiceDefinitions = new List<ApiServiceDefinition>();
            //   iHostBuilder.Properties[key] = apiServiceDefinitions;
            //}

            iHostBuilder.ConfigureWebHost(configure =>
            {
               configure
                  .UseKestrel(serverOptions =>
                  {
                     IEnumerable<ApiServiceDefinition> apiServiceDefinitions = serverOptions.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>();

                     foreach (var apiServiceDefinition in apiServiceDefinitions)
                     {
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

                     IEnumerable<ApiServiceDefinition> apiServiceDefinitions = tempServiceProvider.GetService<IEnumerable<ApiServiceDefinition>>();

                     services
                        .AddRouting()
                        .AddSwaggerGen(setup =>
                        {
                           foreach (var apiServiceDefinition in apiServiceDefinitions)
                           {
                              setup.SwaggerDoc($"{apiServiceDefinition.SwaggerPath}-{apiServiceDefinition.Version}", new OpenApiInfo { Title = apiServiceDefinition.Name, Version = apiServiceDefinition.Version });
                           }
                        });

                     var mvcBuilder = services
                        .AddControllers()
                        .AddJsonOptions(options =>
                        {
                           options.JsonSerializerOptions.WriteIndented = true;
                           options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        });

                     IEnumerable<Assembly> assembliesToScaffold = tempServiceProvider.GetService<IEnumerable<IMithrilShard>>()
                        .Select(shard => shard.GetType().Assembly);
                     // .Concat(_devAssemblyScaffolder?.GetAssemblies());

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
                        IEnumerable<ApiServiceDefinition> apiServiceDefinitions = app.ApplicationServices.GetService<IEnumerable<ApiServiceDefinition>>();

                        foreach (var apiServiceDefinition in apiServiceDefinitions)
                        {
                           setup.SwaggerEndpoint($"/swagger/{apiServiceDefinition.SwaggerPath}-{apiServiceDefinition.Version}/swagger.json", apiServiceDefinition.Name);
                        }
                     })
                     .UseEndpoints(endpoints => endpoints.MapControllers())
                  );
            });
         });

         return forgeBuilder;
      }
   }
}