using System;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core {
   /// <summary>
   /// Group up the core services needed by most of the component
   /// </summary>
   public interface ICoreServices {
      /// <summary>
      /// Gets the logger factory used to create logger.
      /// </summary>
      public ILoggerFactory LoggerFactory { get; }

      /// <summary>
      /// Gets the event bus.
      /// </summary>
      public IEventBus EventBus { get; }

      /// <summary>
      /// Gets the forge lifetime.
      /// </summary>
      public IForgeLifetime ForgeLifetime { get; }

      public IDataFolders DataFolders { get; }

      /// <summary>
      /// Creates the logger for a given <typeparamref name="TLoggedType"/>.
      /// </summary>
      /// <typeparam name="TLoggedType">The type of the component that need to a logger instance.</typeparam>
      /// <returns>ILogger instance</returns>
      public ILogger<TLoggedType> CreateLogger<TLoggedType>();


      /// <summary>
      /// Creates the logger for a given <paramref name="loggedType"/>.
      /// </summary>
      /// <param name="loggedType">The type of the component that need to a logger instance.</param>
      /// <returns>ILogger instance</returns>
      public ILogger CreateLoggerOf(Type loggedType);


      /// <summary>
      /// Creates the logger for the component <paramref name="componentToBeLogged"/>.
      /// </summary>
      /// <param name="componentToBeLogged">The component to be logged.</param>
      /// <returns>ILogger instance</returns>
      public ILogger CreateLoggerFor(object componentToBeLogged);
   }
}
