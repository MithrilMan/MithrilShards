using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MithrilShards.Core;
using MithrilShards.Core.Forge;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// Useful to include these assemblies that didn't have an entry point and wouldn't be loaded.
      /// <returns></returns>
      public static IForgeBuilder UseDevController(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>(
            (hostBuildContext, services) =>
            {
               services.AddSingleton<ApiServiceDefinition>(sp =>
               {
                  var settings = sp.GetService<IOptions<DevControllerSettings>>()!.Value;

                  var definition = new ApiServiceDefinition
                  {
                     Enabled = settings.Enabled,
                     Area = WebApiArea.AREA_DEV,
                     Name = "Dev API",
                     Description = "API useful for debug purpose.",
                     Version = "v1",
                  };

                  forgeBuilder.AddApiService(definition);

                  return definition;
               });
            });

         return forgeBuilder;
      }
   }
}