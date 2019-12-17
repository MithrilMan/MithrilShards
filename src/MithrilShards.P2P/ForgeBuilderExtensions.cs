using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.P2P.Network.Server;

namespace MithrilShards.P2P {
   public static class ForgeBuilderExtensions {
      public static IForgeBuilder UseP2PForgeServer(this IForgeBuilder forgeBuilder) {
         forgeBuilder.AddShard<P2PForgeServer, ForgeServerSettings>(
            (hostBuildContext, services) => {
               services
                  .Replace(ServiceDescriptor.Transient<IForgeServer, P2PForgeServer>()) //replace fake forgeServer with real one
                  .AddSingleton<IServerPeerFactory, ServerPeerFactory>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
