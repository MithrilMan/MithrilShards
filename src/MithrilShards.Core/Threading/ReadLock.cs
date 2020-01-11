using System;
using System.Threading;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Helper class to enter a Read Lock over a <see cref="ReaderWriterLockSlim"/> with an using pattern.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public readonly struct ReadLock : IDisposable
   {
      private readonly ReaderWriterLockSlim theLock;

      public ReadLock(ReaderWriterLockSlim theLock)
      {
         this.theLock = theLock;
         this.theLock.EnterReadLock();
      }

      public void Dispose()
      {
         this.theLock.ExitReadLock();
      }
   }
}
