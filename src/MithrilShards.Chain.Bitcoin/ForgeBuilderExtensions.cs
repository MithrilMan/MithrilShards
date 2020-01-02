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
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <param name="minimumSupportedVersion">The minimum version local nodes requires in order to connect to other peers.</param>
      /// <param name="currentVersion">The current version local peer aim to use with connected peers.</param>
      /// <returns></returns>
      public static IForgeBuilder UseBitcoinChain<TChainDefinition>(this IForgeBuilder forgeBuilder,
                                                                    int minimumSupportedVersion,
                                                                    int currentVersion) where TChainDefinition : class, IChainDefinition {
         forgeBuilder.AddShard<BitcoinShard, BitcoinSettings>(
            (hostBuildContext, services) => {
               services
                  .AddSingleton<IChainDefinition, TChainDefinition>()
                  .AddSingleton(new NodeImplementation(minimumSupportedVersion, currentVersion))
                  .AddSingleton<SelfConnectionTracker>()
                  .Replace(ServiceDescriptor.Singleton<IPeerContextFactory, BitcoinPeerContextFactory>())
                  .AddPeerGuards()
                  .AddMessageSerializers()
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

      private static IServiceCollection AddMessageSerializers(this IServiceCollection services) {
         services
            .AddSingleton<INetworkMessageSerializer, VersionMessageSerializer>()
            .AddSingleton<INetworkMessageSerializer, VerackMessageSerializer>()
            .AddSingleton<INetworkMessageSerializer, RejectMessageSerializer>()
            .AddSingleton<INetworkMessageSerializer, GetaddrMessageSerializer>()
            .AddSingleton<INetworkMessageSerializer, PingMessageSerializer>()
            .AddSingleton<INetworkMessageSerializer, PongMessageSerializer>()
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