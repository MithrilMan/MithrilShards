using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Shards;

namespace MithrilShards.Core.Forge;

public class DefaultForge : BackgroundService, IForge
{
   private readonly IForgeDataFolderLock _forgeDataFolderLock;
   readonly IEnumerable<IMithrilShard> _mithrilShards;
   readonly DefaultConfigurationWriter? _defaultConfigurationManager;
   private readonly ILogger _logger;

   public DefaultForge(ILogger<DefaultForge> logger,
                IForgeDataFolderLock forgeDataFolderLock,
                IEnumerable<IMithrilShard> mithrilShards,
                DefaultConfigurationWriter? defaultConfigurationManager = null)
   {
      _forgeDataFolderLock = forgeDataFolderLock;
      _mithrilShards = mithrilShards;
      _defaultConfigurationManager = defaultConfigurationManager;
      _logger = logger;
   }

   private async Task InitializeShardsAsync(CancellationToken stoppingToken)
   {
      //if no default configuration file is present, create one
      _defaultConfigurationManager?.GenerateDefaultFile();

      using (_logger.BeginScope("Initializing Shards"))
      {
         foreach (IMithrilShard shard in _mithrilShards)
         {
            _logger.LogDebug("Initializing Shard {ShardType}", shard.GetType().Name);
            await shard.InitializeAsync(stoppingToken).ConfigureAwait(false);
         }
      }

      foreach (IMithrilShard shard in _mithrilShards)
      {
         _logger.LogDebug("Starting Shard {ShardType}", shard.GetType().Name);
         _ = shard.StartAsync(stoppingToken);
      }
   }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      if (!_forgeDataFolderLock.TryLockDataFolder())
      {
         _logger.LogCritical("Node folder is being used by another instance of the application!");
         throw new Exception("Node folder is being used!");
      }

      await InitializeShardsAsync(stoppingToken).ConfigureAwait(false);

      _forgeDataFolderLock.UnlockDataFolder();
   }

   public override async Task StopAsync(CancellationToken cancellationToken)
   {
      using IDisposable logScope = _logger.BeginScope("Shutting down the Forge.");

      _logger.LogDebug("Stopping Shards");
      foreach (IMithrilShard shard in _mithrilShards)
      {
         _logger.LogDebug("Stopping Shard {ShardType}", shard.GetType().Name);
         _ = shard.StopAsync(cancellationToken);
      }

      _logger.LogDebug("Stopping Forge instance.");
      await base.StopAsync(cancellationToken).ConfigureAwait(false);
   }

   public List<(string name, string version)> GetMeltedShardsNames()
   {
      if (_mithrilShards.Count() == 0) return new List<(string name, string version)>();

      return _mithrilShards.Select(shard => (
         name: shard.GetType().Name,
         version: shard.GetType().Assembly.GetName().Version?.ToString(3) ?? "-"
         )).ToList();
   }
}
