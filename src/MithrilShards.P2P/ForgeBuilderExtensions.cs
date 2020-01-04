using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Network.Legacy.Server;

namespace MithrilShards.Network.Legacy
{
   public static class ForgeBuilderExtensions
   {
      public static IForgeBuilder UseP2PForgeServer(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<P2PForgeServer, ForgeConnectivitySettings>(
            (hostBuildContext, services) =>
            {
               services
                  .Replace(ServiceDescriptor.Transient<IForgeConnectivity, P2PForgeServer>()) //replace fake forgeServer with real one
                  .AddSingleton<IServerPeerFactory, ServerPeerFactory>()
                  .AddSingleton<IConnectivityPeerStats, ConnectivityPeerStats>()
                  .AddSingleton<IPeerConnectionFactory, PeerConnectionFactory>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
