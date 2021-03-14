using System.Collections.Generic;
using MithrilShards.Core.Shards;
using MithrilShards.Example.Network.Client;

namespace MithrilShards.Example
{
   public class ExampleSettings : MithrilShardSettingsBase
   {
      const long DEFAULT_MAX_TIME_ADJUSTMENT = 70 * 60;

      public long MaxTimeAdjustment { get; set; } = DEFAULT_MAX_TIME_ADJUSTMENT;

      public List<ExampleClientPeerBinding> Connections { get; } = new List<ExampleClientPeerBinding>();
   }
}