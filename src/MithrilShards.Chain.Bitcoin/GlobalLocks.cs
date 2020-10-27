using System.Threading;
using MithrilShards.Core.Threading;
using Microsoft.VisualStudio.Threading;
using System;
using System.Threading.Tasks;

namespace MithrilShards.Chain.Bitcoin
{
   /// <summary>
   /// Temporary class to hold locks that mimic bitcoin core locks
   /// </summary>
   public static class GlobalLocks
   {
      private static readonly ReaderWriterLockSlim cs_main = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      private static readonly AsyncReaderWriterLock cs_main_async = new AsyncReaderWriterLock(false);


      //public static WriteLock WriteOnMain()
      //{
      //   return new WriteLock(cs_main);
      //}

      //public static ReadLock ReadOnMain()
      //{
      //   return new ReadLock(cs_main);
      //}

      public static AsyncReaderWriterLock.Awaitable WriteOnMainAsync()
      {
         return cs_main_async.WriteLockAsync();
      }

      public static AsyncReaderWriterLock.Awaitable ReadOnMainAsync()
      {
         return cs_main_async.ReadLockAsync();
      }
   }
}
