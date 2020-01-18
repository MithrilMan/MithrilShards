using System;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core.Network
{
   /// <summary>
   /// UserAgent is meant to be a way to identify the Forge version and shard it uses, where applicable.
   /// This implementation returns a string composed by the Forge version plus a list of shards used, separated by ;
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.IUserAgentBuilder" />
   public class UserAgentBuilder : IUserAgentBuilder
   {
      protected readonly string forgeVersion;
      protected readonly string shards;

      public UserAgentBuilder(IForge forge)
      {
         Type forgeType = forge.GetType();
         string coreVersion = typeof(IForge).Assembly.GetName().Version?.ToString(3) ?? "-";
         string forgeVersion = $"{ forgeType.Name }:{ forgeType.Assembly.GetName().Version?.ToString(3) ?? "-"}";
         this.forgeVersion = $"MithrilShards:{coreVersion}({forgeVersion})";

         List<(string name, string version)> shardsInfo = forge.GetMeltedShardsNames();
         this.shards = shardsInfo?.Count == 0 ? string.Empty : $"({string.Join("; ", forge.GetMeltedShardsNames().Select(shard => $"{shard.name}:{shard.version}"))})";
      }

      /// <summary>
      /// Gets the user agent.
      /// </summary>
      /// <param name="includeShards">if set to <c>true</c> include shards name and version.</param>
      /// <returns></returns>
      public virtual string GetUserAgent()
      {
         return $"/{this.forgeVersion}{this.shards}/";
      }
   }
}
