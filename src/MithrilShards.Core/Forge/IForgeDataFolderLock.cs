namespace MithrilShards.Core.Forge
{
   public interface IForgeDataFolderLock
   {
      bool TryLockNodeFolder();
      void UnlockNodeFolder();
   }
}