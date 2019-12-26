using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.EventBus;
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

      public bool Enabled { get; private set; }

      public BaseProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus) {
         this.logger = logger;
         this.eventBus = eventBus;

         this.eventBusSubscriptionsTokens = new List<ISubscription>();
      }

      public virtual void Attach(IPeerContext peerContext) {
         this.PeerContext = peerContext as BitcoinPeerContext ?? throw new ArgumentException("Expected BitcoinPeerContext", nameof(peerContext));
         this.messageWriter = this.PeerContext.GetMessageWriter();
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

      public virtual void Dispose() {
         foreach (SubscriptionToken token in this.eventBusSubscriptionsTokens) {
            token?.Dispose();
         }
      }
   }
}
