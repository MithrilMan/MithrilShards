using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Forge {
   public class Forge : IForge {
      private readonly ICoreServices coreServices;
      private readonly IForgeDataFolderLock forgeDataFolderLock;
      private readonly ILogger logger;
      private readonly IForgeLifetime forgeLifetime;

      public Forge(ICoreServices coreServices, IForgeDataFolderLock forgeDataFolderLock) {
         this.coreServices = coreServices;
         this.forgeDataFolderLock = forgeDataFolderLock;
         this.logger = coreServices.CreateLogger<Forge>();
         this.forgeLifetime = coreServices.ForgeLifetime;
      }

      public CancellationToken ForgeShuttingDown => throw new System.NotImplementedException();


      public void Start() {
         this.forgeLifetime.SetState(ForgeState.Starting);

         if (!this.forgeDataFolderLock.TryLockNodeFolder()) {
            this.logger.LogCritical("Node folder is being used by another instance of the application!");
            throw new Exception("Node folder is being used!");
         }

         //// Initialize all registered features.
         //this.fullNodeFeatureExecutor.Initialize();

         //// Initialize peer connection.
         //var consensusManager = this.Services.ServiceProvider.GetRequiredService<IConsensusManager>();
         //this.ConnectionManager.Initialize(consensusManager);

         // Fire INodeLifetime.Started.
         this.forgeLifetime.SetState(ForgeState.Started);

         //this.StartPeriodicLog();

         //this.State = FullNodeState.Started;
      }

      public void ShutDown() {
         if (this.forgeLifetime.State == ForgeState.ShuttingDown || this.forgeLifetime.State == ForgeState.ShuttedDown) {
            return;
         }

         this.forgeLifetime.SetState(ForgeState.ShuttingDown);

         this.forgeLifetime.ShutDown();

         // TODO: complete the shutdown sequence disposing components that aren't forgeLifetime aware
         // or not subscribed to shutting down events on event bus

         this.forgeDataFolderLock.UnlockNodeFolder();

         this.forgeLifetime.SetState(ForgeState.ShuttedDown);
      }

      #region IDisposable Support
      private bool disposedValue = false; // To detect redundant calls

      protected virtual void Dispose(bool disposing) {
         if (!this.disposedValue) {
            if (disposing) {
               this.ShutDown();
            }

            this.disposedValue = true;
         }
      }

      // This code added to correctly implement the disposable pattern.
      public void Dispose() {
         this.Dispose(true);
      }
      #endregion


   }
}
