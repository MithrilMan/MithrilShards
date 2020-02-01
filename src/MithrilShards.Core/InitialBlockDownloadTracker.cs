using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core
{
   /// <summary>
   /// temporary Mock, to be moved and implemented properly
   /// </summary>
   public class InitialBlockDownloadTracker : IInitialBlockDownloadTracker
   {
      readonly ILogger<InitialBlockDownloadTracker> logger;
      readonly IEventBus eventBus;
      readonly EventSubscriptionManager subscriptionManager = new EventSubscriptionManager();

      public InitialBlockDownloadTracker(ILogger<InitialBlockDownloadTracker> logger, IEventBus eventBus)
      {
         this.logger = logger;
         this.eventBus = eventBus;

         //TODO register to tip advance
         //this.subscriptionManager.RegisterSubscriptions(this.eventBus.Subscribe())
      }

      public bool IsDownloadingBlocks()
      {
         return true;
      }
   }
}
