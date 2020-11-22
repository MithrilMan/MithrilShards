using System.IO;

namespace MithrilShards.Core.Forge
{
   /// <summary>
   /// Class that prevents another instance of the node to run in the same data folder
   /// and allows external applications to see if the node is running.
   /// </summary>
   public class ForgeDataFolderLock : IForgeDataFolderLock
   {
      private const string LOCK_FILE_NAME = "lockfile";
      private readonly string _lockFileName;

      private FileStream? _fileStream;

      public ForgeDataFolderLock(IDataFolders dataFolders)
      {
         this._lockFileName = Path.Combine(dataFolders.RootPath, LOCK_FILE_NAME);
      }

      public bool TryLockDataFolder()
      {
         try
         {
            this._fileStream = new FileStream(this._lockFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            return true;
         }
         catch (IOException)
         {
            return false;
         }
      }

      public void UnlockDataFolder()
      {
         this._fileStream?.Close();
         try
         {
            File.Delete(this._lockFileName);
         }
         catch { }
      }
   }
}
