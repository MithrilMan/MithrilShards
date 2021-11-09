using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Shards;

/// <summary>
/// Interface used to have a fallback mechanism to generate default <see cref="IMithrilShard"/> settings
/// if <see cref="IForge"/> configuration file is missing
/// </summary>
public interface IMithrilShardSettings : IValidatableObject
{

   /// <summary>
   /// Gets the configuration section used to read from configuration from.
   /// </summary>
   /// <value>
   /// The configuration section used to read from configuration from.
   /// </value>
   [JsonIgnore]
   public string ConfigurationSection { get; }
}
