using System.Net;
using MithrilShards.Core.Shards;
using MithrilShards.Core.Shards.Validation.ValidationAttributes;

namespace MithrilShards.EventDispatcher.SignalR;

public class SignalrEventDispatcherSettings : MithrilShardSettingsBase
{
   /// <summary>IP address and port number where SignalR Event Dispatcher Hub is listening.</summary>
   [IPEndPointValidator]
   public string EndPoint { get; set; } = "127.0.0.1:45023";

   /// <summary>
   /// Gets or sets a value indicating whether this <see cref="SignalrEventDispatcherSettings"/> is enabled.
   /// </summary>
   /// <value>
   ///   <c>true</c> if enabled; otherwise, <c>false</c>.
   /// </value>
   public bool Enabled { get; set; } = true;

   public IPEndPoint GetIPEndPoint()
   {
      return IPEndPoint.Parse(EndPoint);
   }
}
