using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Network.Server.Guards;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Chain.Bitcoin
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <param name="minimumSupportedVersion">The minimum version local nodes requires in order to connect to other peers.</param>
      /// <param name="currentVersion">The current version local peer aim to use with connected peers.</param>
      /// <returns></returns>
      public static IForgeBuilder UseBitcoinChain(this IForgeBuilder forgeBuilder,
                                                  string networkName,
                                                  int minimumSupportedVersion,
                                                  int currentVersion)
      {
         if (forgeBuilder is null) throw new ArgumentNullException(nameof(forgeBuilder));

         Type? chainDefinitionType = networkName.ToLowerInvariant() switch
         {
            "bitcoin-main" => typeof(BitcoinMainDefinition),
            "bitcoin-testnet" => typeof(BitcoinTestnetDefinition),
            "bitcoin-regtest" => typeof(BitcoinRegtestDefinition),
            _ => null
         };

         if (chainDefinitionType == null) ThrowHelper.ThrowArgumentException($"Unknown Network {networkName.ToLowerInvariant()}");

         forgeBuilder.AddShard<BitcoinShard, BitcoinSettings>(
            (hostBuildContext, services) =>
            {
               services
                  .AddSingleton(typeof(IChainDefinition), chainDefinitionType)
                  .AddSingleton<IConsensusParameters>(serviceProvider => serviceProvider.GetRequiredService<IChainDefinition>().Consensus)
                  .AddSingleton<INetworkDefinition>(serviceProvider => serviceProvider.GetRequiredService<IChainDefinition>().NetworkDefinition)
                  .AddSingleton(new NodeImplementation(minimumSupportedVersion, currentVersion))
                  .AddSingleton<IHeadersTree, HeadersTree>()
                  .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                  .AddSingleton<IChainState, ChainState>()
                  .AddSingleton<ICoinsView, CoinsView>()
                  .AddSingleton<IHeadersTree, HeadersTree>()
                  .AddSingleton<IInitialBlockDownloadTracker, InitialBlockDownloadTracker>()
                  .AddSingleton<IHeaderMedianTimeCalculator, HeaderMedianTimeCalculator>()
                  .AddSingleton<IBlockHeaderRepository, InMemoryBlockHeaderRepository>()
                  .AddSingleton<IBlockFetcherManager, BlockFetcherManager>()
                  .AddSingleton<IValidationRulesChecker, ValidationRulesChecker>()
                  .AddSingleton<IHeaderValidator, HeaderValidator>()
                  .AddHostedService(sp => sp.GetRequiredService<IHeaderValidator>())
                  .AddSingleton<IHeaderValidationContextFactory, HeaderValidationContextFactory>()
                  .AddSingleton<ILocalServiceProvider, LocalServiceProvider>()
                  .AddSingleton<SelfConnectionTracker>()
                  .Replace(ServiceDescriptor.Singleton<IPeerContextFactory, BitcoinPeerContextFactory>())
                  .Replace(ServiceDescriptor.Singleton<IUserAgentBuilder, BitcoinUserAgentBuilder>())
                  .Replace(ServiceDescriptor.Singleton<IBlockHeaderHashCalculator, BlockHeaderHashCalculator>())
                  .AddPeerGuards()
                  .AddMessageSerializers()
                  .AddProtocolTypeSerializers()
                  .AddValidationRules()
                  .AddMessageProcessors();
            });

         return forgeBuilder;
      }
      private static IServiceCollection AddValidationRules(this IServiceCollection services)
      {
         services
            .AddSingleton<IHeaderValidationRule, CheckPreviousBlock>()
            .AddSingleton<IHeaderValidationRule, CheckProofOfWork>()
            .AddSingleton<IHeaderValidationRule, CheckBlockTime>()
            ;

         return services;
      }

      private static IServiceCollection AddPeerGuards(this IServiceCollection services)
      {
         services
            .AddSingleton<IServerPeerConnectionGuard, InitialBlockDownloadStateGuard>()
            .AddSingleton<IServerPeerConnectionGuard, MaxConnectionThresholdGuard>()
            ;

         return services;
      }

      private static IServiceCollection AddMessageSerializers(this IServiceCollection services)
      {
         // discover and register all message serializer in this assembly
         Type serializerInterface = typeof(INetworkMessageSerializer);
         foreach (Type messageSerializerType in typeof(BitcoinShard).Assembly.GetTypes().Where(t => serializerInterface.IsAssignableFrom(t)))
         {
            services.AddSingleton(typeof(INetworkMessageSerializer), messageSerializerType);
         }

         return services;
      }

      private static IServiceCollection AddProtocolTypeSerializers(this IServiceCollection services)
      {
         Type protocolSerializerInterface = typeof(IProtocolTypeSerializer<>);
         var implementations = from type in typeof(BitcoinShard).Assembly.GetTypes()
                               from typeInterface in type.GetInterfaces()
                               where typeInterface.IsGenericType && protocolSerializerInterface.IsAssignableFrom(typeInterface.GetGenericTypeDefinition())
                               select new { Interface = typeInterface, ImplementationType = type };

         foreach (var implementation in implementations)
         {
            services.AddSingleton(implementation.Interface, implementation.ImplementationType);
         }

         return services;
      }

      private static IServiceCollection AddMessageProcessors(this IServiceCollection services)
      {
         // discover and register all message serializer in this assembly
         Type serializerInterface = typeof(INetworkMessageProcessor);
         foreach (Type processorType in typeof(BitcoinShard).Assembly.GetTypes().Where(t => !t.IsAbstract && serializerInterface.IsAssignableFrom(t)))
         {
            services.AddTransient(typeof(INetworkMessageProcessor), processorType);
         }

         return services;
      }
   }
}