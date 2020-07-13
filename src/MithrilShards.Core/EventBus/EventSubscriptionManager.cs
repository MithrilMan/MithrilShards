using System;
using System.Collections.Generic;

namespace MithrilShards.Core.EventBus
{
   /// <summary>
   ///  Manage registrations of subscribed <see cref="IEventBus"/> event handlers.
   ///  The class is thread safe.
   ///  Must be disposed to unregister subscriptions.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public class EventSubscriptionManager : IDisposable
   {
      private readonly object subscriptionsLock = new object();
      private readonly List<SubscriptionToken> subscriptionTokens = new List<SubscriptionToken>();

      /// <summary>
      /// Registers the provided subscriptions.
      /// The operation is thread safe.
      /// </summary>
      /// <param name="subscriptions">The subscription action.</param>
      public EventSubscriptionManager RegisterSubscriptions(params SubscriptionToken[] subscriptions)
      {
         if (this.disposedValue) throw new ObjectDisposedException(nameof(EventSubscriptionManager));

         lock (this.subscriptionsLock)
         {
            this.subscriptionTokens.AddRange(subscriptions);
         }

         return this;
      }

      #region IDisposable Support
      private bool disposedValue = false;

      protected virtual void Dispose(bool disposing)
      {
         if (!this.disposedValue)
         {
            if (disposing)
            {
               lock (this.subscriptionsLock)
               {
                  foreach (SubscriptionToken token in this.subscriptionTokens)
                  {
                     token?.Dispose();
                  }
                  this.subscriptionTokens.Clear();
               }
            }

            this.disposedValue = true;
         }
      }

      // This code added to correctly implement the disposable pattern.
      public void Dispose()
      {
         this.Dispose(true);
      }
      #endregion


   }
}
