using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Forge;

namespace MithrilShards.Core {
   /// <summary>
   /// Group up the core services needed by most of the component
   /// </summary>
   public class CoreServices : ICoreServices {
      public ILoggerFactory LoggerFactory { get; }

      public IEventBus EventBus { get; }

      public IForgeLifetime ForgeLifetime { get; }

      public IDataFolders DataFolders { get; }

      public CoreServices(ILoggerFactory loggerFactory, IEventBus eventBus, IForgeLifetime forgeLifetime, IDataFolders dataFolders) {
         this.LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
         this.EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
         this.ForgeLifetime = forgeLifetime ?? throw new ArgumentNullException(nameof(forgeLifetime));
         this.DataFolders = dataFolders ?? throw new ArgumentNullException(nameof(dataFolders));
      }

      public ILogger<TLoggedType> CreateLogger<TLoggedType>() {
         return this.LoggerFactory.CreateLogger<TLoggedType>();
      }

      public ILogger CreateLoggerOf(Type loggedType) {
         return this.LoggerFactory.CreateLogger(loggedType);
      }

      public ILogger CreateLoggerFor(object componentToBeLogged) {
         return this.LoggerFactory.CreateLogger(componentToBeLogged.GetType());
      }
   }
}
