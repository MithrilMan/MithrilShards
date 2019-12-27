//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Connections;
//using Microsoft.Extensions.Logging;

//namespace MithrilShards.Network.Bedrock.Middleware {
//   class ConnectionTimeoutMiddleware {
//      private readonly ConnectionDelegate next;
//      private readonly SemaphoreSlim limiter;
//      private readonly ILogger logger;

//      public ConnectionTimeoutMiddleware(ConnectionDelegate next, ILogger logger, int limit) {
//         this.next = next;
//         this.logger = logger;
//         this.limiter = new SemaphoreSlim(limit);
//      }

//      public async Task OnConnectionAsync(ConnectionContext connectionContext) {
//         // Wait 10 seconds for a connection
//         var task = limiter.WaitAsync(TimeSpan.FromSeconds(10));

//         if (!task.IsCompletedSuccessfully) {
//            logger.LogInformation("{ConnectionId} queued", connectionContext.ConnectionId);

//            if (!await task) {
//               logger.LogInformation("{ConnectionId} timed out in the connection queue", connectionContext.ConnectionId);
//               return;
//            }
//         }

//         try {
//            await next(connectionContext);
//         }
//         finally {
//            limiter.Release();
//         }
//      }
//   }
//}
