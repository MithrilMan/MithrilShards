using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Network.Server.Guards;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Processors;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Chain.Bitcoin {
   public static class ForgeBuilderExtensions {
      public static IForgeBuilder UseBitcoinChain(this IForgeBuilder forgeBuilder) {
         forgeBuilder.AddShard<BitcoinShard, BitcoinSettings>(
            (hostBuildContext, services) => {
               services
                  .AddSingleton<IChainDefinition, BitcoinMainDefinition>()
                  .AddSingleton<INetworkMessageSerializer, VersionMessageSerializer>()
                  .Replace(ServiceDescriptor.Singleton<IPeerContextFactory, BitcoinPeerContextFactory>())
                  .AddPeerGuards()
                  .AddMessageProcessors();
            });

         return forgeBuilder;
      }

      private static IServiceCollection AddPeerGuards(this IServiceCollection services) {
         services
            .AddSingleton<IServerPeerConnectionGuard, InitialBlockDownloadStateGuard>()
            .AddSingleton<IServerPeerConnectionGuard, MaxConnectionThresholdGuard>()
            ;

         return services;
      }

      private static IServiceCollection AddMessageProcessors(this IServiceCollection services) {
         services
            .AddTransient<INetworkMessageProcessor, HandshakeProcessor>()
            ;

         return services;
      }
   }
}