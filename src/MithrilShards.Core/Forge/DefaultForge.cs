using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Forge;

public class DefaultForge(ILogger<DefaultForge> logger,
             IForgeDataFolderLock forgeDataFolderLock,
             IEnumerable<IMithrilShard> mithrilShards,
             DefaultConfigurationWriter? defaultConfigurationManager = null) : BackgroundService, IForge
{

   private async Task InitializeShardsAsync(CancellationToken stoppingToken)
   {
      //if no default configuration file is present, create one
      defaultConfigurationManager?.GenerateDefaultFile();

      using (logger.BeginScope("Initializing Shards"))
      {
         foreach (IMithrilShard shard in mithrilShards)
         {
            logger.LogDebug("Initializing Shard {ShardType}", shard.GetType().Name);
            await shard.InitializeAsync(stoppingToken).ConfigureAwait(false);
         }
      }

      foreach (IMithrilShard shard in mithrilShards)
      {
         logger.LogDebug("Starting Shard {ShardType}", shard.GetType().Name);
         _ = shard.StartAsync(stoppingToken);
      }
   }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      if (!forgeDataFolderLock.TryLockDataFolder())
      {
         logger.LogCritical("Node folder is being used by another instance of the application!");
         throw new Exception("Node folder is being used!");
      }

      await InitializeShardsAsync(stoppingToken).ConfigureAwait(false);

      forgeDataFolderLock.UnlockDataFolder();
   }

   public override async Task StopAsync(CancellationToken cancellationToken)
   {
      using var logScope = logger.BeginScope("Shutting down the Forge.");

      logger.LogDebug("Stopping Shards");

      // use wait all to wait for all shards to stop
      var tasks = mithrilShards.Select(shard =>
      {
         logger.LogDebug("Stopping Shard {ShardType}", shard.GetType().Name);
         return shard.StopAsync(cancellationToken);
      }).ToArray();

      await Task.WhenAll(tasks).ConfigureAwait(false);

      logger.LogDebug("Stopping Forge instance.");
      await base.StopAsync(cancellationToken).ConfigureAwait(false);
   }

   public List<(string name, string version)> GetMeltedShardsNames()
   {
      if (!mithrilShards.Any()) return [];

      return mithrilShards.Select(shard => (
         name: shard.GetType().Name,
         version: shard.GetType().Assembly.GetName().Version?.ToString(3) ?? "-"
         )).ToList();
   }
}
