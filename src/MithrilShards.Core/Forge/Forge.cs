using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Core.Forge {
   public class Forge : BackgroundService, IForge {
      private readonly IForgeDataFolderLock forgeDataFolderLock;
      readonly IForgeServer forgeServer;
      readonly IEnumerable<IMithrilShard> mithrilShards;
      readonly DefaultConfigurationWriter defaultConfigurationManager;
      private readonly ILogger logger;

      public IServiceProvider Services => throw new NotImplementedException();

      public Forge(ILogger<Forge> logger, IForgeDataFolderLock forgeDataFolderLock, IForgeServer forgeServer, IEnumerable<IMithrilShard> mithrilShards, DefaultConfigurationWriter defaultConfigurationManager = null) {
         this.forgeDataFolderLock = forgeDataFolderLock;
         this.forgeServer = forgeServer;
         this.mithrilShards = mithrilShards;
         this.defaultConfigurationManager = defaultConfigurationManager;
         this.logger = logger;
      }

      private async Task InitializeShardsAsync(CancellationToken stoppingToken) {
         //if no default configuration file is present, create one
         this.defaultConfigurationManager?.GenerateDefaultFile();

         using (this.logger.BeginScope("Initializing Shards")) {
            foreach (IMithrilShard shard in this.mithrilShards) {
               await shard.InitializeAsync(stoppingToken).ConfigureAwait(false);
            }
         }

         using (this.logger.BeginScope("Starting Shards")) {
            foreach (IMithrilShard shard in this.mithrilShards) {
               _ = shard.StartAsync(stoppingToken);
            }
         }
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
         using (this.logger.BeginScope("Locking Data Folder")) {
            if (!this.forgeDataFolderLock.TryLockNodeFolder()) {
               this.logger.LogCritical("Node folder is being used by another instance of the application!");
               throw new Exception("Node folder is being used!");
            }
         }

         await this.InitializeShardsAsync(stoppingToken).ConfigureAwait(false);

         await this.forgeServer.InitializeAsync(stoppingToken).ConfigureAwait(false);

         await this.forgeServer.StartAsync(stoppingToken).ConfigureAwait(false);

         using (this.logger.BeginScope("Unlocking Data Folder")) {
            this.forgeDataFolderLock.UnlockNodeFolder();
         }
      }

      public override async Task StopAsync(CancellationToken cancellationToken) {
         using (this.logger.BeginScope("Stopping forge server.")) {
            await this.forgeServer.StopAsync(cancellationToken);
         }

         using (this.logger.BeginScope("Stopping Forge instance.")) {
            await base.StopAsync(cancellationToken);
         }
      }
   }
}
