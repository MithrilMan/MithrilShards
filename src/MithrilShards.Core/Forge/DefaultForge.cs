﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;
using MithrilShards.Core.Network;

namespace MithrilShards.Core.Forge
{
   public class DefaultForge : BackgroundService, IForge
   {
      private readonly IForgeDataFolderLock forgeDataFolderLock;
      readonly IForgeConnectivity forgeServer;
      readonly IEnumerable<IMithrilShard> mithrilShards;
      readonly DefaultConfigurationWriter? defaultConfigurationManager;
      private readonly ILogger logger;

      public DefaultForge(ILogger<DefaultForge> logger,
                   IForgeDataFolderLock forgeDataFolderLock,
                   IForgeConnectivity forgeServer,
                   IEnumerable<IMithrilShard> mithrilShards,
                   DefaultConfigurationWriter? defaultConfigurationManager = null)
      {
         this.forgeDataFolderLock = forgeDataFolderLock;
         this.forgeServer = forgeServer;
         this.mithrilShards = mithrilShards;
         this.defaultConfigurationManager = defaultConfigurationManager;
         this.logger = logger;
      }

      private async Task InitializeShardsAsync(CancellationToken stoppingToken)
      {
         //if no default configuration file is present, create one
         this.defaultConfigurationManager?.GenerateDefaultFile();

         using (this.logger.BeginScope("Initializing Shards"))
         {
            foreach (IMithrilShard shard in this.mithrilShards)
            {
               if (!(shard is IForgeConnectivity))
               {
                  this.logger.LogDebug("Initializing Shard {ShardType}", shard.GetType().Name);
                  await shard.InitializeAsync(stoppingToken).ConfigureAwait(false);
               }
            }
         }

         foreach (IMithrilShard shard in this.mithrilShards)
         {
            if (!(shard is IForgeConnectivity))
            {
               this.logger.LogDebug("Starting Shard {ShardType}", shard.GetType().Name);
               _ = shard.StartAsync(stoppingToken);
            }
         }
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
         if (!this.forgeDataFolderLock.TryLockDataFolder())
         {
            this.logger.LogCritical("Node folder is being used by another instance of the application!");
            throw new Exception("Node folder is being used!");
         }

         await this.InitializeShardsAsync(stoppingToken).ConfigureAwait(false);

         await this.forgeServer.InitializeAsync(stoppingToken).ConfigureAwait(false);

         await this.forgeServer.StartAsync(stoppingToken).ConfigureAwait(false);

         this.forgeDataFolderLock.UnlockDataFolder();
      }

      public override async Task StopAsync(CancellationToken cancellationToken)
      {
         using IDisposable logScope = this.logger.BeginScope("Shutting down the Forge.");

         this.logger.LogDebug("Stopping forge server.");
         await this.forgeServer.StopAsync(cancellationToken).ConfigureAwait(false);

         this.logger.LogDebug("Stopping Shards");
         foreach (IMithrilShard shard in this.mithrilShards)
         {
            if (!(shard is IForgeConnectivity))
            {
               this.logger.LogDebug("Stopping Shard {ShardType}", shard.GetType().Name);
               _ = shard.StopAsync(cancellationToken);
            }
         }

         this.logger.LogDebug("Stopping Forge instance.");
         await base.StopAsync(cancellationToken).ConfigureAwait(false);
      }

      public List<(string name, string version)> GetMeltedShardsNames()
      {
         if (this.mithrilShards?.Count() == 0) return new List<(string name, string version)>();

         return this.mithrilShards.Select(shard => (
            name: shard.GetType().Name,
            version: shard.GetType().Assembly.GetName().Version?.ToString(3) ?? "-"
            )).ToList();
      }
   }
}