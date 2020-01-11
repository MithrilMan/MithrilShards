using System;
using System.Threading;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Helper class to enter a Write Lock over a <see cref="ReaderWriterLockSlim"/> with an using pattern.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public readonly struct WriteLock : IDisposable
   {
      private readonly ReaderWriterLockSlim theLock;

      public WriteLock(ReaderWriterLockSlim theLock)
      {
         this.theLock = theLock;
         this.theLock.EnterWriteLock();
      }

      public void Dispose()
      {
         this.theLock.ExitWriteLock();
      }
   }
}
