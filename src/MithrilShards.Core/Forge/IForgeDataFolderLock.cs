namespace MithrilShards.Core.Forge
{
   public interface IForgeDataFolderLock
   {
      bool TryLockDataFolder();
      void UnlockDataFolder();
   }
}