using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   /// <summary>
   /// Allow to set and get available node services.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.ILocalServiceProvider" />
   public class LocalServiceProvider : ILocalServiceProvider
   {
      readonly ILogger<LocalServiceProvider> _logger;

      private NodeServices _availableServices;

      public LocalServiceProvider(ILogger<LocalServiceProvider> logger)
      {
         _logger = logger;

         AddServices(NodeServices.Network | NodeServices.NetworkLimited | NodeServices.Witness);
      }

      public void AddServices(NodeServices services)
      {
         _logger.LogDebug("Adding services {NodeServices}", services);
         _availableServices |= services;
      }

      public void RemoveServices(NodeServices services)
      {
         _logger.LogDebug("Removing services {NodeServices}", services);
         _availableServices &= ~services;
      }

      public NodeServices GetServices()
      {
         return _availableServices;
      }

      public bool HasServices(NodeServices service)
      {
         return _availableServices.HasFlag(service);
      }
   }
}
