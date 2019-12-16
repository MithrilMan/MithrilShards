using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MithrilShards.Core.Extensions {
   public static class ILoggerExtensions {
      /// <summary>
      /// Ensures the returned log isn't null, returning NullLogger if it's actually null.
      /// </summary>
      /// <param name="logger">The logger.</param>
      /// <returns></returns>
      public static ILogger NullCheck(this ILogger logger) {
         return logger ?? NullLogger.Instance;
      }
   }
}
