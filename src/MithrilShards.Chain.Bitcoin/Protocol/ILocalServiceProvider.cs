using MithrilShards.Chain.Bitcoin.Network;

namespace MithrilShards.Chain.Bitcoin.Protocol;

/// <summary>
/// Methods to set and get available node services
/// </summary>
public interface ILocalServiceProvider
{
   /// <summary>
   /// Adds <paramref name="services"/> to already available node services.
   /// </summary>
   /// <param name="services">The services.</param>
   void AddServices(NodeServices services);

   /// <summary>
   /// Removes the specified services.
   /// </summary>
   /// <param name="services">The services to remove.</param>
   void RemoveServices(NodeServices services);

   /// <summary>
   /// Gets the available node services.
   /// </summary>
   /// <returns></returns>
   NodeServices GetServices();

   /// <summary>
   /// Determines whether the specified services are available in current node.
   /// </summary>
   /// <param name="services">The services to check.</param>
   /// <returns>
   ///   <c>true</c> if the specified service is available; otherwise, <c>false</c>.
   /// </returns>
   bool HasServices(NodeServices services);
}
