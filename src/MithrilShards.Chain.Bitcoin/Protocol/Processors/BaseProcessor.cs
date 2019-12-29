using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors {
   public abstract class BaseProcessor : INetworkMessageProcessor {
      protected readonly ILogger<BaseProcessor> logger;
      protected readonly IEventBus eventBus;

      protected INetworkMessageWriter messageWriter;

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly List<ISubscription> eventBusSubscriptionsTokens;

      public BitcoinPeerContext PeerContext { get; private set; }

      public bool Enabled { get; private set; } = true;

      public BaseProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus) {
         this.logger = logger;
         this.eventBus = eventBus;

         this.eventBusSubscriptionsTokens = new List<ISubscription>();
      }

      public virtual ValueTask AttachAsync(IPeerContext peerContext) {
         this.PeerContext = peerContext as BitcoinPeerContext ?? throw new ArgumentException("Expected BitcoinPeerContext", nameof(peerContext));
         this.messageWriter = this.PeerContext.GetMessageWriter();
         return default;
      }

      public abstract Task<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation);

      /// <summary>
      /// Registers the component life time subscription to an <see cref="IEventBus"/> event that will be automatically
      /// unregistered once the component gets disposed.
      /// </summary>
      /// <param name="subscription">The subscription.</param>
      protected void RegisterLifeTimeSubscription(ISubscription subscription) {
         this.eventBusSubscriptionsTokens.Add(subscription);
      }

      /// <summary>
      /// Disconnects if, after timeout expires, the condition is evaluated to true.
      /// </summary>
      /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
      /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
      /// <param name="cancellation">The cancellation that may interrupt the <paramref name="condition"/> evaluation.</param>
      /// <returns></returns>
      public Task DisconnectIfAsync(Func<bool> condition, TimeSpan timeout, string reason, CancellationToken cancellation = default) {
         if (cancellation == default) {
            cancellation = this.PeerContext.ConnectionCancellationTokenSource.Token;
         }
         return Task.Run(async () => {
            await Task.Delay(timeout).WithCancellationAsync(cancellation).ConfigureAwait(false);
            // if cancellation was requested, return without doing anything
            if (cancellation.IsCancellationRequested) {
               return;
            }
            else {
               if (condition() && !this.PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested) {
                  this.logger.LogDebug("Request peer disconnection because {DisconnectionRequestReason}", reason);
                  this.PeerContext.ConnectionCancellationTokenSource.Cancel();
               }
            }
         });
      }

      public virtual void Dispose() {
         foreach (SubscriptionToken token in this.eventBusSubscriptionsTokens) {
            token?.Dispose();
         }

         this.PeerContext.ConnectionCancellationTokenSource.Cancel();
      }
   }
}
