using Microsoft.Extensions.DependencyInjection;

namespace MithrilShards.WebApi;

public static class IServiceCollectionExtensions
{
   /// <summary>
   /// Extension used by other shards to create custom <see cref="ApiServiceDefinition" />.
   /// </summary>
   /// <param name="services">The services collection.</param>
   /// <param name="apiServiceDefinition">The API service definition.</param>
   /// <returns></returns>
   public static IServiceCollection AddApiServiceDefinition(this IServiceCollection services, ApiServiceDefinition apiServiceDefinition)
   {
      services.AddSingleton<ApiServiceDefinition>(apiServiceDefinition);

      return services;
   }
}
