//using System;
//using System.Collections.Generic;
//using System.Text;
//using Microsoft.Extensions.Logging;

//namespace MithrilShards.Chain.BitcoinTests
//{
//   public static class LoggerUtils
//   {
//      public static Mock<ILogger<T>> LoggerMock<T>() where T : class
//      {
//         return new Mock<ILogger<T>>();
//      }

//      /// <summary>
//      /// Returns an <pre>ILogger<T></pre> as used by the Microsoft.Logging framework.
//      /// You can use this for constructors that require an ILogger parameter.
//      /// </summary>
//      /// <typeparam name="T"></typeparam>
//      /// <returns></returns>
//      public static ILogger<T> Logger<T>() where T : class
//      {
//         return LoggerMock<T>().Object;
//      }

//      public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string message, string failMessage = null)
//      {
//         loggerMock.VerifyLog(level, message, Times.Once(), failMessage);
//      }
//      public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string message, Times times, string failMessage = null)
//      {
//         loggerMock.Verify(l => l.Log<Object>(level, It.IsAny<EventId>(), It.Is<Object>(o => o.ToString() == message), null, It.IsAny<Func<Object, Exception, String>>()), times, failMessage);
//      }

//   }
//}
