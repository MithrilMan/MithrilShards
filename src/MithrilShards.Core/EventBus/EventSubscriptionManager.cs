using System;
using System.Collections.Generic;

namespace MithrilShards.Core.EventBus;

/// <summary>
///  Manage registrations of subscribed <see cref="IEventBus"/> event handlers.
///  The class is thread safe.
///  Must be disposed to unregister subscriptions.
/// </summary>
/// <seealso cref="System.IDisposable" />
public class EventSubscriptionManager : IDisposable
{
   private readonly object _subscriptionsLock = new();
   private readonly List<SubscriptionToken> _subscriptionTokens = new();

   /// <summary>
   /// Registers the provided subscriptions.
   /// The operation is thread safe.
   /// </summary>
   /// <param name="subscriptions">The subscription action.</param>
   public EventSubscriptionManager RegisterSubscriptions(params SubscriptionToken[] subscriptions)
   {
      if (_disposedValue) throw new ObjectDisposedException(nameof(EventSubscriptionManager));

      lock (_subscriptionsLock)
      {
         _subscriptionTokens.AddRange(subscriptions);
      }

      return this;
   }

   #region IDisposable Support
   private bool _disposedValue = false;

   protected virtual void Dispose(bool disposing)
   {
      if (!_disposedValue)
      {
         if (disposing)
         {
            lock (_subscriptionsLock)
            {
               foreach (SubscriptionToken token in _subscriptionTokens)
               {
                  token?.Dispose();
               }
               _subscriptionTokens.Clear();
            }
         }

         _disposedValue = true;
      }
   }

   // This code added to correctly implement the disposable pattern.
   public void Dispose()
   {
      Dispose(true);
   }
   #endregion


}
