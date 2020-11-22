using System;
using System.Threading;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Helper class to enter a Write Lock over a <see cref="ReaderWriterLockSlim"/> with an using pattern.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public class WriteLock : IDisposable
   {
      private readonly ReaderWriterLockSlim _theLock;

      public WriteLock(ReaderWriterLockSlim theLock)
      {
         _theLock = theLock;
         _theLock.EnterWriteLock();
      }

      public void Dispose()
      {
         _theLock.ExitWriteLock();
      }
   }
}
