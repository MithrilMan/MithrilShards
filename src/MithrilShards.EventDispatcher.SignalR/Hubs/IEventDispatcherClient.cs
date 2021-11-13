using System.Threading.Tasks;

namespace MithrilShards.EventDispatcher.SignalR.Hubs;

/// <summary>
/// Defines client methods that can be invoked from the EventDispatcherHub
/// </summary>
public interface IEventDispatcherClient
{
   Task<object> EventPublished(string eventName);
}
