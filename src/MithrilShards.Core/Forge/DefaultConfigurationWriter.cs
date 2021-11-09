using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Forge;

/// <summary>
/// Used to generate a default configuration file containing all discovered IMithrilShardSettings default values.
/// </summary>
public class DefaultConfigurationWriter
{
   readonly ILogger<DefaultConfigurationWriter> _logger;
   readonly IEnumerable<IMithrilShardSettings> _mithrilShardSettings;
   readonly string _configurationFilePath;

   public DefaultConfigurationWriter(ILogger<DefaultConfigurationWriter> logger, IEnumerable<IMithrilShardSettings> mithrilShardSettings, string configurationFilePath)
   {
      _logger = logger;
      _mithrilShardSettings = mithrilShardSettings;
      _configurationFilePath = configurationFilePath;
   }



   /// <summary>
   /// Generates the default configuration file populating it with default <see cref="IMithrilShardSettings"/>
   /// values discovered in current Forge instance.
   /// </summary>
   public void GenerateDefaultFile()
   {
      if (_mithrilShardSettings == null)
      {
         _logger.LogInformation("No Mithril Shard settings found in current Forge.");
         return;
      }

      var appConfig = new Dictionary<string, object>();

      foreach (IMithrilShardSettings shardSettings in _mithrilShardSettings)
      {
         appConfig[shardSettings.ConfigurationSection] = shardSettings;
      }

      System.IO.File.WriteAllText(_configurationFilePath, JsonSerializer.Serialize(appConfig, new JsonSerializerOptions
      {
         WriteIndented = true,
      }));
   }
}
