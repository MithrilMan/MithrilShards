using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.MithrilShards {
   /// <summary>
   /// Interface used to have a fallback mechanism to generate default <see cref="IMithrilShard"/> settings 
   /// if <see cref="IForge"/> configuration file is missing
   /// </summary>
   public abstract class MithrilShardSettingsBase : IMithrilShardSettings {
      string IMithrilShardSettings.ConfigurationSection {
         get => this.GetType().Name;
      }
   }
}