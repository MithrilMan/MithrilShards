using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge
{
   /// <summary>
   /// Used to generate a default configuration file containing all discovered IMithrilShardSettings default values.
   /// </summary>
   public class DefaultConfigurationWriter
   {
      readonly ILogger<DefaultConfigurationWriter> logger;
      readonly IEnumerable<IMithrilShardSettings> mithrilShardSettings;
      readonly string configurationFilePath;

      public DefaultConfigurationWriter(ILogger<DefaultConfigurationWriter> logger, IEnumerable<IMithrilShardSettings> mithrilShardSettings, string configurationFilePath)
      {
         this.logger = logger;
         this.mithrilShardSettings = mithrilShardSettings;
         this.configurationFilePath = configurationFilePath;
      }



      /// <summary>
      /// Generates the default configuration file populating it with default <see cref="IMithrilShardSettings"/> 
      /// values discovered in current Forge instance.
      /// </summary>
      public void GenerateDefaultFile()
      {
         if (this.mithrilShardSettings == null)
         {
            this.logger.LogInformation("No Mithril Shard settings found in current Forge.");
            return;
         }

         Dictionary<string, object> appConfig = new Dictionary<string, object>();

         foreach (IMithrilShardSettings shardSettings in this.mithrilShardSettings)
         {
            appConfig[shardSettings.ConfigurationSection] = shardSettings;
         }

         System.IO.File.WriteAllText(this.configurationFilePath, JsonSerializer.Serialize(appConfig, new JsonSerializerOptions
         {
            WriteIndented = true,
         }));
      }
   }
}
