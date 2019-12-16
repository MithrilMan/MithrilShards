using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Class that prevents another instance of the node to run in the same data folder
   /// and allows external applications to see if the node is running.
   /// </summary>
   public class ForgeDataFolderLock : IForgeDataFolderLock {
      private const string LOCK_FILE_NAME = "lockfile";
      private readonly string lockFileName;

      private FileStream fileStream;

      public ForgeDataFolderLock(IDataFolders dataFolders) {
         this.lockFileName = Path.Combine(dataFolders.RootPath, LOCK_FILE_NAME);
      }

      public bool TryLockNodeFolder() {
         try {
            this.fileStream = new FileStream(this.lockFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            return true;
         }
         catch (IOException) {
            return false;
         }
      }

      public void UnlockNodeFolder() {
         this.fileStream.Close();
         File.Delete(this.lockFileName);
      }
   }
}
