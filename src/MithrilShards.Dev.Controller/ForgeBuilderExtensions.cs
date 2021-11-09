using MithrilShards.Core.Forge;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller;

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
      forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>((context, services) =>
      {
         if (context.GetShardSettings<DevControllerSettings>()!.Enabled)
         {
            services.AddApiServiceDefinition(new ApiServiceDefinition
            {
               Area = WebApiArea.AREA_DEV,
               Name = "Dev API",
               Description = "API useful for debug purpose.",
               Version = "v1",
            });
         }
      });

      return forgeBuilder;
   }
}
