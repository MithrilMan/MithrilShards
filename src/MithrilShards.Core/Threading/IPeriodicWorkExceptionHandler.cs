using System;

namespace MithrilShards.Core.Threading
{
   public interface IPeriodicWorkExceptionHandler
   {
      void OnException(IPeriodicWork failedWork, Exception ex, out bool continueExecution);
   }
}
