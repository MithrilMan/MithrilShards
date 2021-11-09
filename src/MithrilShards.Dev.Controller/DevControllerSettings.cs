using MithrilShards.Core.Shards;

namespace MithrilShards.Dev.Controller;

public class DevControllerSettings : MithrilShardSettingsBase
{
   /// <summary>
   /// Gets or sets a value indicating whether this <see cref="DevControllerSettings"/> is enabled.
   /// </summary>
   /// <value>
   ///   <c>true</c> if enabled; otherwise, <c>false</c>.
   /// </value>
   public bool Enabled { get; set; } = true;
}
