using System;
using System.Threading;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Helper class to enter a Read Lock over a <see cref="ReaderWriterLockSlim"/> with an using pattern.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public class ReadLock : IDisposable
   {
      private readonly ReaderWriterLockSlim _theLock;

      public ReadLock(ReaderWriterLockSlim theLock)
      {
         this._theLock = theLock;
         this._theLock.EnterReadLock();
      }

      public void Dispose()
      {
         this._theLock.ExitReadLock();
      }
   }
}
