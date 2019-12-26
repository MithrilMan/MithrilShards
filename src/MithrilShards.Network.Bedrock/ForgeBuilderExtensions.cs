using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Network.Bedrock {
   public static class ForgeBuilderExtensions {
      public static IForgeBuilder UseBedrockForgeServer(this IForgeBuilder forgeBuilder) {
         forgeBuilder.AddShard<BedrockForgeServer, ForgeServerSettings>(
            (hostBuildContext, services) => {
               services
                  .Replace(ServiceDescriptor.Singleton<IForgeServer, BedrockForgeServer>())
                  .AddSingleton<IServerPeerStats, ServerPeerStats>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
