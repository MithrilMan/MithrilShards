using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Core.Forge.MithrilShards {
   public interface IMithrilShardDefinition {
      string Name { get; }

      string Description { get; }

      List<string> Tags { get; }

      List<Type> Dependencies { get; }

      /// <summary>
      /// Type of the feature startup class.
      /// If it implements ConfigureServices method it will be invoked to configure the shard services (similar from AspNetCore design)
      /// </summary>
      Type StartupType { get; }

      /// <summary>Type of the feature class.</summary>
      Type FeatureType { get; }
   }
}
