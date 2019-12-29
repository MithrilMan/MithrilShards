using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Network.Bedrock {
   public static class ForgeBuilderExtensions {
      public static IForgeBuilder UseBedrockForgeServer(this IForgeBuilder forgeBuilder) {
         if (forgeBuilder is null) {
            throw new System.ArgumentNullException(nameof(forgeBuilder));
         }

         forgeBuilder.AddShard<BedrockForgeConnectivity, ForgeConnectivitySettings>(
            (hostBuildContext, services) => {
               services
                  .Replace(ServiceDescriptor.Singleton<IForgeConnectivity, BedrockForgeConnectivity>())
                  .AddSingleton<IServerPeerStats, ServerPeerStats>()
                  .AddSingleton<MithrilForgeClientConnectionHandler>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
