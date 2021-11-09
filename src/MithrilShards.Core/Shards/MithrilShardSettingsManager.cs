using Microsoft.Extensions.Configuration;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Shards;

/// <summary>
/// Interface used to have a fallback mechanism to generate default <see cref="IMithrilShard"/> settings
/// if <see cref="IForge"/> configuration file is missing
/// </summary>
public static class MithrilShardSettingsManager
{
   /// <summary>
   /// Gets the section name used by the specified <typeparamref name="TMithrilShardSetting"/> shard setting.
   /// </summary>
   /// <typeparam name="TMithrilShardSetting">The type of the Mithril Shard setting to get config Section from.</typeparam>
   /// <returns></returns>
   public static IConfigurationSection GetSection<TMithrilShardSetting>(IConfiguration configuration) where TMithrilShardSetting : IMithrilShardSettings, new()
   {
      return configuration.GetSection(new TMithrilShardSetting().ConfigurationSection);
   }
}
