using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Core.Threading
{
   /// <summary>
   /// Tracks instantiated IPeriodicWork to check their health status and collect statistics
   /// </summary>
   public interface IPeriodicWorkTracker
   {
      void StartTracking(IPeriodicWork work);

      void StopTracking(IPeriodicWork work);
   }
}
