using System.Diagnostics.CodeAnalysis;
using System.Net;
using MithrilShards.Core;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.WebApi
{
   public class WebApiSettings : MithrilShardSettingsBase
   {
      /// <summary>IP address and port number on which the shard will serve its Web API endpoint.</summary>
      [DisallowNull]
      public string? EndPoint { get; set; } = "127.0.0.1:45020";

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

      public void ValidateEndPoint(out IPEndPoint ipEndPoint)
      {
         if (!IPEndPoint.TryParse(EndPoint, out ipEndPoint))
         {
            ThrowHelper.ThrowArgumentException($"Wrong configuration parameter for {nameof(EndPoint)}");
         }
      }

      public string GetListeningUrl()
      {
         return $"{(Https ? "https" : "http")}://{EndPoint}";
      }
   }
}