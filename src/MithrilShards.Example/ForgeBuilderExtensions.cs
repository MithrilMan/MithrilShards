using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;
using MithrilShards.Example.Network;
using MithrilShards.Example.Network.Client;
using MithrilShards.Example.Network.Server.Guards;

namespace MithrilShards.Example;

public static class ForgeBuilderExtensions
{
   /// <summary>
   /// Inject the Example shard.
   /// </summary>
   /// <param name="forgeBuilder">The forge builder.</param>
   /// <param name="minimumSupportedVersion">The minimum version local nodes requires in order to connect to other peers.</param>
   /// <param name="currentVersion">The current version local peer aim to use with connected peers.</param>
   /// <returns></returns>
   public static IForgeBuilder UseExample(this IForgeBuilder forgeBuilder, int minimumSupportedVersion, int currentVersion)
   {
      ArgumentNullException.ThrowIfNull(forgeBuilder);

      forgeBuilder.AddShard<ExampleShard, ExampleSettings>(
         (hostBuildContext, services) =>
         {
            services
               .AddSingleton(new NodeImplementation(minimumSupportedVersion, currentVersion))
               .AddSingleton<IDateTimeProvider, DateTimeProvider>()
               .AddSingleton<IQuoteService, QuoteService>()
               .AddPeerGuards()
               .AddMessageSerializers()
               .AddProtocolTypeSerializers()
               .AddMessageProcessors()
               .ReplaceServices();
         });

      return forgeBuilder;
   }


   private static IServiceCollection AddPeerGuards(this IServiceCollection services)
   {
      services
         .AddSingleton<IServerPeerConnectionGuard, MaxConnectionThresholdGuard>()
         .AddSingleton<IServerPeerConnectionGuard, BannedPeerGuard>()
         ;

      return services;
   }

   private static IServiceCollection AddMessageSerializers(this IServiceCollection services)
   {
      // Discovers and registers all message serializer in this assembly.
      // It is possible to add them manually to have full control of message serializers we are interested into.
      Type serializerInterface = typeof(INetworkMessageSerializer);
      foreach (Type messageSerializerType in typeof(ExampleShard).Assembly.GetTypes().Where(t => serializerInterface.IsAssignableFrom(t) && !t.IsAbstract))
      {
         services.AddSingleton(typeof(INetworkMessageSerializer), messageSerializerType);
      }

      return services;
   }

   private static IServiceCollection AddProtocolTypeSerializers(this IServiceCollection services)
   {
      // Discovers and registers all message serializer in this assembly.
      // It is possible to add them manually to have full control of protocol serializers we are interested into.
      Type protocolSerializerInterface = typeof(IProtocolTypeSerializer<>);
      var implementations = from type in typeof(ExampleShard).Assembly.GetTypes()
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
      // Discovers and registers all message processors in this assembly.
      // It is possible to add them manually to have full control of message processors we are interested into.
      Type serializerInterface = typeof(INetworkMessageProcessor);
      foreach (Type processorType in typeof(ExampleShard).Assembly.GetTypes().Where(t => !t.IsAbstract && serializerInterface.IsAssignableFrom(t)))
      {
         services.AddTransient(typeof(INetworkMessageProcessor), processorType);
      }

      return services;
   }

   private static IServiceCollection ReplaceServices(this IServiceCollection services)
   {
      services
         .Replace(ServiceDescriptor.Singleton<IPeerContextFactory, ExamplePeerContextFactory>())
         ;

      /// Replace extension is useful when we want to replace an implementation that's only used as a single entry when resolved, and not as IEnumerable,
      /// because Replace, as per documentation, replaces the first registered service type with the provided ServiceDescriptor.
      /// when we have different implementation of the same service type, when we resolve a single instance then the last registered implementation is used,
      /// while if we resolve as IEnumerable then all known implementations are returned. This mean that when we are registered something to be resolved as
      /// IEnumerable we can't replace one of them at will so we have to fall back to the Remove method and then add the one implementation we want, back into
      /// the IServiceCollection.

      // in this scenario, we are looking for the ServiceDescriptor that registers our RequiredConnection implementation of IConnector
      ServiceDescriptor? requiredConnectionDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IConnector) && descriptor.ImplementationType == typeof(RequiredConnection));
      if (requiredConnectionDescriptor != null)
      {
         // remove it
         services.Remove(requiredConnectionDescriptor);
      }

      // and add back our implementation
      services.AddSingleton<IConnector, ExampleRequiredConnection>();

      return services;
   }
}
