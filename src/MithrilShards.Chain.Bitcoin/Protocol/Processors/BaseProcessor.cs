using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public abstract class BaseProcessor : INetworkMessageProcessor
   {
      protected readonly ILogger<BaseProcessor> logger;
      protected readonly IEventBus eventBus;
      protected readonly IPeerBehaviorManager peerBehaviorManager;

      private INetworkMessageWriter messageWriter;

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager eventSubscriptionManager = new EventSubscriptionManager();

      public BitcoinPeerContext PeerContext { get; private set; }

      public bool Enabled { get; private set; } = true;

      public BaseProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerBehaviorManager = peerBehaviorManager;

      }

      public virtual ValueTask AttachAsync(IPeerContext peerContext)
      {
         this.PeerContext = peerContext as BitcoinPeerContext ?? throw new ArgumentException("Expected BitcoinPeerContext", nameof(peerContext));
         this.messageWriter = this.PeerContext.GetMessageWriter();
         return default;
      }

      /// <summary>
      /// Registers the component life time subscription to an <see cref="IEventBus"/> event that will be automatically
      /// unregistered once the component gets disposed.
      /// </summary>
      /// <param name="subscription">The subscription.</param>
      protected void RegisterLifeTimeSubscription(SubscriptionToken subscription)
      {
         this.eventSubscriptionManager.RegisterSubscriptions(subscription);
      }

      /// <summary>
      /// Disconnects if, after timeout expires, the condition is evaluated to true.
      /// </summary>
      /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
      /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
      /// <param name="cancellation">The cancellation that may interrupt the <paramref name="condition"/> evaluation.</param>
      /// <returns></returns>
      public Task DisconnectIfAsync(Func<bool> condition, TimeSpan timeout, string reason, CancellationToken cancellation = default)
      {
         if (cancellation == default)
         {
            cancellation = this.PeerContext.ConnectionCancellationTokenSource.Token;
         }
         return Task.Run(async () =>
         {
            await Task.Delay(timeout).WithCancellationAsync(cancellation).ConfigureAwait(false);
            // if cancellation was requested, return without doing anything
            if (!cancellation.IsCancellationRequested && !this.PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested && condition())
            {
               this.logger.LogDebug("Request peer disconnection because {DisconnectionRequestReason}", reason);
               this.PeerContext.ConnectionCancellationTokenSource.Cancel();
            }
         });
      }

      public virtual void Dispose()
      {
         this.eventSubscriptionManager.Dispose();

         this.PeerContext.ConnectionCancellationTokenSource.Cancel();
      }

      /// <summary>
      /// Sends the message asynchronously to the other peer.
      /// </summary>
      /// <param name="message">The message to send.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      public async ValueTask SendMessageAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         await this.messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
      }

      /// <summary>
      /// Sends the message asynchronously to the other peer.
      /// </summary>
      /// <param name="message">The message to send.</param>
      /// <param name="minVersion">
      /// The minimum, inclusive, negotiated version required to send this message.
      /// Passing 0 means the message will be sent without version check.
      /// </param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      public async ValueTask<bool> SendMessageAsync(int minVersion, INetworkMessage message, CancellationToken cancellationToken = default)
      {
         if (minVersion == 0 || this.PeerContext.NegotiatedProtocolVersion.Version >= minVersion)
         {
            await this.messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            return true;
         }
         else
         {
            return false;
         }
      }
   }
}
