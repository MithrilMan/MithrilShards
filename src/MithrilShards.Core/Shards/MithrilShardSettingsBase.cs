using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Shards;

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

   /// <summary>
   /// Determines whether the specified object is valid.
   /// Allows for custom validation when the simple DataAnnotationValidateOptions using DataAnnotation is not enough (e.g. complex objects aren't automatically checked)
   /// </summary>
   /// <param name="validationContext">The validation context.</param>
   /// <returns>
   /// A collection that holds failed-validation information.
   /// </returns>
   public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => [];
}
