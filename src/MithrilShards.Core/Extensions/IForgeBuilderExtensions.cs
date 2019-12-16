using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Extensions {
   public static class IForgeBuilderExtensions {

      public static IForgeBuilder UseForge(this IForgeBuilder forgeBuilder) {
         forgeBuilder.RegisterServices(services => {
            services
            .AddSingleton<IDataFolders, DataFolders>(serviceProvider => new DataFolders("."))
            .AddSingleton<IForgeLifetime, ForgeLifetime>()
            .AddSingleton<IForgeDataFolderLock, ForgeDataFolderLock>()
            .AddSingleton<IEventBus, InMemoryEventBus>()
            .AddSingleton<ISubscriptionErrorHandler, DefaultSubscriptionErrorHandler>()
            .AddSingleton<ILoggerFactory>(serviceProvider => {
               return LoggerFactory.Create(builder => {
                  builder.AddConsole();
               });
            })
            .AddTransient(typeof(ILogger<>), (typeof(Logger<>)))
            .AddSingleton<ICoreServices, CoreServices>()
            .AddSingleton<IForge, Forge.Forge>();
         });

         return forgeBuilder;
      }
   }
}
