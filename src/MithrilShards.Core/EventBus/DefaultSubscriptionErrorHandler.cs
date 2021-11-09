using System;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.EventBus;

/// <summary>
/// Default implementation of <see cref="ISubscriptionErrorHandler"/> that log the error and re-throw it.
/// </summary>
/// <seealso cref="ISubscriptionErrorHandler" />
public class DefaultSubscriptionErrorHandler : ISubscriptionErrorHandler
{
   /// <summary>
   /// The logger
   /// </summary>
   private readonly ILogger _logger;

   public DefaultSubscriptionErrorHandler(ILogger<DefaultSubscriptionErrorHandler> logger)
   {
      _logger = logger;
   }

   /// <inheritdoc />
   public void Handle(EventBase theEvent, Exception exception, ISubscription subscription)
   {
      _logger.LogError(exception, "Error handling the event {0}", theEvent.GetType().Name);
      throw exception;
   }
}
