using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Forge;

public interface IForge : IHostedService
{
   /// <summary>
   /// Gets the melted shards names and version list.
   /// </summary>
   /// <returns></returns>
   List<(string name, string version)> GetMeltedShardsNames();
}
