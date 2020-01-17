using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.PeerAddressBook;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Core
{
   internal static class ForgeBuilderExtensions
   {

      public static IServiceCollection ConfigureForge(this IServiceCollection services, HostBuilderContext context)
      {
         services
               // data folder
               .AddSingleton<IDataFolders, DataFolders>(serviceProvider => new DataFolders("."))
               .AddSingleton<IForgeDataFolderLock, ForgeDataFolderLock>()

               // event bus
               .AddSingleton<IEventBus, InMemoryEventBus>()
               .AddSingleton<ISubscriptionErrorHandler, DefaultSubscriptionErrorHandler>()

               // miscellaneous
               .AddSingleton<IDateTimeProvider, DateTimeProvider>()
               .AddSingleton<IInitialBlockDownloadState, InitialBlockDownloadState>()
               .AddSingleton<IRandomNumberGenerator, DefaultRandomNumberGenerator>()
               .AddSingleton<IUserAgentBuilder, UserAgentBuilder>()

               .ConfigureNetwork()
               ;

         return services;
      }

      private static IServiceCollection ConfigureNetwork(this IServiceCollection services)
      {
         services
            .AddSingleton<IForgeConnectivity, FakeForgeConnectivity>()
            .AddSingleton<INetworkMessageSerializerManager, NetworkMessageSerializerManager>()
            .AddSingleton<INetworkMessageProcessorFactory, NetworkMessageProcessorFactory>()
            .AddSingleton<IPeerContextFactory, PeerContextFactory<PeerContext>>()

            // client
            .AddSingleton<IConnectionManager, ConnectionManager>()
            .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<IConnectionManager>())
            .AddSingleton<IConnector, RequiredConnection>()

            //peer address book
            .AddSingleton<IPeerAddressBook, DefaultPeerAddressBook>()

            //with peer behavior manager
            .AddSingleton<IPeerBehaviorManager, DefaultPeerBehaviorManager>()
            .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<IPeerBehaviorManager>())
            ;

         return services;
      }
   }
}