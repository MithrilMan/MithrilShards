using System.Net;
using MithrilShards.Core.Shards;
using MithrilShards.Core.Shards.Validation.ValidationAttributes;

namespace MithrilShards.WebApi
{
   public class WebApiSettings : MithrilShardSettingsBase
   {
      /// <summary>IP address and port number on which the shard will serve its Web API endpoint.</summary>
      [IPEndPointValidator]
      public string EndPoint { get; set; } = "127.0.0.1:45020";

      /// <summary>
      /// Gets or sets a value indicating whether this <see cref="WebApiSettings"/> is enabled.
      /// </summary>
      /// <value>
      ///   <c>true</c> if enabled; otherwise, <c>false</c>.
      /// </value>
      public bool Enabled { get; set; } = true;

      /// <summary>
      /// Gets or sets a value indicating whether WEB APIs should be exposed on HTTPS.
      /// </summary>
      /// <value>
      ///   <c>true</c> if using HTTPS; otherwise, <c>false</c>.
      /// </value>
      public bool Https { get; set; } = false;

      public string GetListeningUrl()
      {
         return $"{(Https ? "https" : "http")}://{EndPoint}";
      }

      public IPEndPoint GetIPEndPoint()
      {
         return IPEndPoint.Parse(EndPoint);
      }
   }
}