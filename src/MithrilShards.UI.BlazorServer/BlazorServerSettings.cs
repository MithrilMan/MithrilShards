using System.ComponentModel.DataAnnotations;
using System.Net;
using MithrilShards.Core.Shards;
using MithrilShards.Core.Shards.Validation.ValidationAttributes;

namespace MithrilShards.UI.BlazorServer
{
   public class BlazorServerSettings : MithrilShardSettingsBase
   {
      /// <summary>IP address and port number on which the shard will serve its Web API endpoint.</summary>
      [IPEndPointValidator]
      public string EndPoint { get; set; } = "127.0.0.1:45022";

      /// <summary>
      /// Gets or sets a value indicating whether this <see cref="BlazorServerSettings"/> is enabled.
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
}