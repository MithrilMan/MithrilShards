using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Core {
   /// <summary>
   /// Providing date time functionality.
   /// </summary>
   public interface IDateTimeProvider {
      /// <summary>
      /// Get the current time in Linux format.
      /// </summary>
      long GetTime();

      /// <summary>
      /// Get the current time offset in UTC.
      /// </summary>
      DateTimeOffset GetTimeOffset();

      /// <summary>
      /// Get the current time in UTC.
      /// </summary>
      DateTime GetUtcNow();

      /// <summary>
      /// Obtains adjusted time, which is time synced with network peers.
      /// </summary>
      /// <returns>Adjusted UTC timestamp.</returns>
      DateTime GetAdjustedTime();

      /// <summary>
      /// Obtains adjusted time, which is time synced with network peers, as Unix timestamp with seconds precision.
      /// </summary>
      /// <returns>Adjusted UTC timestamp as Unix timestamp with seconds precision.</returns>
      long GetAdjustedTimeAsUnixTimestamp();

      /// <summary>
      /// Sets adjusted time offset, which is time difference from network peers.
      /// </summary>
      /// <param name="adjustedTimeOffset">Offset to adjust time with.</param>
      void SetAdjustedTimeOffset(TimeSpan adjustedTimeOffset);
   }
}
