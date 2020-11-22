using System.Globalization;
using System.Text.Json.Serialization;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.MithrilShards
{
   /// <summary>
   /// Base class to have a fallback mechanism to generate default <see cref="IMithrilShard"/> settings
   /// if <see cref="IForge"/> configuration file is missing
   /// </summary>
   public abstract class MithrilShardSettingsBase : IMithrilShardSettings
   {
      [JsonIgnore]
      public virtual string ConfigurationSection
      {
         get
         {
            const string partToRemove = "settings";
            string name = GetType().Name;
            if (name.EndsWith(partToRemove, true, CultureInfo.InvariantCulture))
            {
               name = name.Substring(0, name.Length - partToRemove.Length);
            }
            return name;
         }
      }
   }
}