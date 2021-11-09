using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MithrilShards.Core.Network.Server;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Network;

public class ForgeConnectivitySettings : MithrilShardSettingsBase
{
   const int DEFAULT_MAX_INBOUND_CONNECTIONS = 20;
   const int DEFAULT_MAX_OUTBOUND_CONNECTIONS = 20;
   const bool DEFAULT_ALLOW_LOOPBACK_CONNECTION = true;
   const int DEFAULT_FORCE_SHUTDOWN_AFTER = 300;
   const int DEFAULT_BAN_SCORE = -100;
   const int DEFAULT_MISBEHAVING_BAN_TIME = 60 * 60 * 24; // 24 hours

   public int MaxInboundConnections { get; set; } = DEFAULT_MAX_INBOUND_CONNECTIONS;

   public int MaxOutboundConnections { get; set; } = DEFAULT_MAX_OUTBOUND_CONNECTIONS;

   public bool AllowLoopbackConnection { get; set; } = DEFAULT_ALLOW_LOOPBACK_CONNECTION;

   /// <summary>
   /// Gets or sets the time the Forge wait for a graceful shutdown, before forcing one (expressed in seconds).
   /// </summary>
   public int ForceShutdownAfter { get; set; } = DEFAULT_FORCE_SHUTDOWN_AFTER;

   public int BanScore { get; set; } = DEFAULT_BAN_SCORE;

   /// <summary>
   /// Gets or sets the ban time (in seconds).
   /// </summary>
   public int MisbehavingBanTime { get; set; } = DEFAULT_MISBEHAVING_BAN_TIME;

   public List<ServerPeerBinding> Listeners { get; } = new List<ServerPeerBinding>();

   public List<ClientPeerBinding> Connections { get; } = new List<ClientPeerBinding>();



   /// <inheritdoc/>
   public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
   {

      for (int i = 0; i < Connections.Count; i++)
      {
         ClientPeerBinding connection = Connections[i];
         List<ValidationResult> results = new List<ValidationResult>();
         Validator.TryValidateObject(connection, new ValidationContext(connection), results, true);
         foreach (var result in results)
         {
            if (result is not null)
            {
               //result.MemberNames = result.MemberNames.Append(new string[] { $"{nameof(Connections)}[{i}]" })
               //yield return result;
               yield return new ValidationResult(result.ErrorMessage, new string[] { $"{nameof(Connections)}[{i}].{result.MemberNames.First()}" });
            }
         }
      }

      for (int i = 0; i < Listeners.Count; i++)
      {
         ServerPeerBinding listener = Listeners[i];
         List<ValidationResult> results = new List<ValidationResult>();
         Validator.TryValidateObject(listener, new ValidationContext(listener), results, true);
         foreach (var result in results)
         {
            if (result is not null)
            {
               //yield return result;
               yield return new ValidationResult(result.ErrorMessage, new string[] { $"{nameof(Listeners)}[{i}].{result.MemberNames.First()}" });
            }
         }
      }
   }
}
