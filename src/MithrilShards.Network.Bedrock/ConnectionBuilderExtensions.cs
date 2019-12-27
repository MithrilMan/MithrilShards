//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace MithrilShards.Network.Bedrock {
//   public static class ConnectionBuilderExtensions {
//      public static TBuilder UseConnectionLimits<TBuilder>(this TBuilder builder, int connectionLimit) where TBuilder : IConnectionBuilder {
//         var loggerFactory = builder.ApplicationServices.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
//         var logger = loggerFactory.CreateLogger<ConnectionLimitMiddleware>();
//         builder.Use(next => new ConnectionLimitMiddleware(next, logger, connectionLimit).OnConnectionAsync);
//         return builder;
//      }
//   }
//}
