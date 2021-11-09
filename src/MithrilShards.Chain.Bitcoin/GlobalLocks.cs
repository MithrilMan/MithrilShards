using Microsoft.VisualStudio.Threading;

namespace MithrilShards.Chain.Bitcoin;

/// <summary>
/// Temporary class to hold locks that mimic bitcoin core locks
/// </summary>
public static class GlobalLocks
{
   private static readonly JoinableTaskContext _joinableTaskContext = new();
   private static readonly AsyncReaderWriterLock _cs_main_async = new(_joinableTaskContext, false);

   public static AsyncReaderWriterLock.Awaitable WriteOnMainAsync()
   {
      return _cs_main_async.WriteLockAsync();
   }

   public static AsyncReaderWriterLock.Awaitable ReadOnMainAsync()
   {
      return _cs_main_async.ReadLockAsync();
   }
}
