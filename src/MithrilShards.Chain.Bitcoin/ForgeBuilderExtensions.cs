using Microsoft.Extensions.DependencyInjection;
using MithrilShards.Chain.Bitcoin.Network.Server.Guards;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Chain.Bitcoin {
   public static class ForgeBuilderExtensions {
      public static IForgeBuilder UseP2PForgeServer(this IForgeBuilder forgeBuilder) {
         forgeBuilder.AddShard<BitcoinShard, BitcoinSettings>(
            (hostBuildContext, services) => {
               services
                  .AddSingleton<INetworkMessageSerializator, BitcoinMessageSerializator>();
               AddPeerGuards(services);
            });

         return forgeBuilder;
      }


      private static void AddPeerGuards(IServiceCollection services) {
         services
            .AddSingleton<IServerPeerConnectionGuard, InitialBlockDownloadStateGuard>()
            .AddSingleton<IServerPeerConnectionGuard, MaxConnectionThresholdGuard>()
            ;
      }
   }
}