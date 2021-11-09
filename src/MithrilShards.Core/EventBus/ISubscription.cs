using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.EventBus
{
   public interface ISubscription
   {
      /// <summary>
      /// Token returned to the subscriber
      /// </summary>
      SubscriptionToken SubscriptionToken { get; }

      /// <summary>
      /// Publish to the subscriber
      /// </summary>
      /// <param name="eventBase">The event base.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      ValueTask ProcessEventAsync(EventBase eventBase, CancellationToken cancellationToken);
   }
}
