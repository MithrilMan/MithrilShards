using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;

namespace MithrilShards.Network.Bedrock
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bedrock forge server as connectivity provider.
      /// </summary>
      /// <typeparam name="TNetworkProtocolMessageSerializer">The type of the network message protocol to use to serialize and deserialize messages.
      /// It has to implement <see cref="INetworkProtocolMessageSerializer"/>.</typeparam>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <returns></returns>
      /// <exception cref="System.ArgumentNullException">forgeBuilder</exception>
      public static IForgeBuilder UseBedrockForgeServer<TNetworkProtocolMessageSerializer>(this IForgeBuilder forgeBuilder) where TNetworkProtocolMessageSerializer : class, INetworkProtocolMessageSerializer
      {
         if (forgeBuilder is null)
         {
            throw new System.ArgumentNullException(nameof(forgeBuilder));
         }

         forgeBuilder.AddShard<BedrockForgeConnectivity, ForgeConnectivitySettings>(
            (hostBuildContext, services) =>
            {
               services
                  .Replace(ServiceDescriptor.Singleton<IForgeConnectivity, BedrockForgeConnectivity>())
                  .AddSingleton<IConnectivityPeerStats, ConnectivityPeerStats>()
                  .AddSingleton<MithrilForgeClientConnectionHandler>()
                  .AddTransient<INetworkProtocolMessageSerializer, TNetworkProtocolMessageSerializer>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
