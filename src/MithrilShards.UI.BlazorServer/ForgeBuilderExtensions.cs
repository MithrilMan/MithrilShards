using MithrilShards.Core.Forge;
using MithrilShards.UI.BlazorServer;

namespace MithrilShards.Core
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <returns></returns>
      public static IForgeBuilder UseBlazorServer(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<BlazorServerShard, BlazorServerSettings>(
            (hostBuildContext, services) =>
            {
            });

         return forgeBuilder;
      }
   }
}