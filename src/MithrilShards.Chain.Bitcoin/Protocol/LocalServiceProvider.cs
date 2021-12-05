using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;

namespace MithrilShards.Chain.Bitcoin.Protocol;

/// <summary>
/// Allow to set and get available node services.
/// </summary>
/// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.ILocalServiceProvider" />
public partial class LocalServiceProvider : ILocalServiceProvider
{
   private NodeServices _availableServices;

   public LocalServiceProvider(ILogger<LocalServiceProvider> logger)
   {
      _logger = logger;

      AddServices(NodeServices.Network | NodeServices.NetworkLimited | NodeServices.Witness);
   }

   public void AddServices(NodeServices services)
   {
      _availableServices |= services;
      DebugServicesAdded(services);
   }

   public void RemoveServices(NodeServices services)
   {
      _availableServices &= ~services;
      DebugServicesRemoved(services);
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

partial class LocalServiceProvider
{
   readonly ILogger<LocalServiceProvider> _logger;

   [LoggerMessage(0, LogLevel.Debug, "Services added: {NodeServices}.")]
   partial void DebugServicesAdded(NodeServices nodeServices);

   [LoggerMessage(0, LogLevel.Debug, "Services removed: {NodeServices}.")]
   partial void DebugServicesRemoved(NodeServices nodeServices);
}
