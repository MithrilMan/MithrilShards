using System.Threading;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin
{
   /// <summary>
   /// Temporary class to hold locks that mimic bitcoin core locks
   /// </summary>
   public static class GlobalLocks
   {
      private static readonly ReaderWriterLockSlim cs_main = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

      public static WriteLock WriteOnMain()
      {
         return new WriteLock(cs_main);
      }

      public static ReadLock ReadOnMain()
      {
         return new ReadLock(cs_main);
      }
   }
}
