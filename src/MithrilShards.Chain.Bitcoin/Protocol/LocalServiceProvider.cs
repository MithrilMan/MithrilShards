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
      readonly ILogger<LocalServiceProvider> logger;

      private NodeServices availableServices;

      public LocalServiceProvider(ILogger<LocalServiceProvider> logger)
      {
         this.logger = logger;

         this.AddServices(NodeServices.Network | NodeServices.NetworkLimited | NodeServices.Witness);
      }

      public void AddServices(NodeServices services)
      {
         this.logger.LogDebug("Adding services {NodeServices}", services);
         this.availableServices |= services;
      }

      public void RemoveServices(NodeServices services)
      {
         this.logger.LogDebug("Removing services {NodeServices}", services);
         this.availableServices &= ~services;
      }

      public NodeServices GetServices()
      {
         return this.availableServices;
      }

      public bool HasServices(NodeServices service)
      {
         return this.availableServices.HasFlag(service);
      }
   }
}
