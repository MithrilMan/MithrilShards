using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Threading;
using MithrilShards.Example.Protocol.Messages;
using MithrilShards.Example.Protocol.Types;

namespace MithrilShards.Example.Protocol.Processors
{
   public partial class PingPongProcessor : BaseProcessor,
      INetworkMessageHandler<PingMessage>,
      INetworkMessageHandler<PongMessage>
   {

      /// <summary>
      /// Time between pings automatically sent out for latency probing and keep-alive (in seconds).
      /// </summary>
      const int PING_INTERVAL = 10;

      /// <summary>
      /// Time after which to disconnect, after waiting for a ping response (in seconds).
      /// </summary>
      const int TIMEOUT_INTERVAL = 20 * 60;

      readonly IRandomNumberGenerator randomNumberGenerator;
      readonly IDateTimeProvider dateTimeProvider;
      readonly IPeriodicWork periodicPing;
      readonly IQuoteService quoteService;
      private CancellationTokenSource pingCancellationTokenSource = null!;

      public PingPongProcessor(ILogger<PingPongProcessor> logger,
                               IEventBus eventBus,
                               IPeerBehaviorManager peerBehaviorManager,
                               IRandomNumberGenerator randomNumberGenerator,
                               IDateTimeProvider dateTimeProvider,
                               IPeriodicWork periodicPing,
                               IQuoteService quoteService)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
      {
         this.randomNumberGenerator = randomNumberGenerator;
         this.dateTimeProvider = dateTimeProvider;
         this.periodicPing = periodicPing;
         this.quoteService = quoteService;
      }

      protected override ValueTask OnPeerHandshakedAsync()
      {
         _ = this.periodicPing.StartAsync(
               label: $"{nameof(periodicPing)}-{PeerContext.PeerId}",
               work: PingAsync,
               interval: TimeSpan.FromSeconds(PING_INTERVAL),
               cancellation: PeerContext.ConnectionCancellationTokenSource.Token
            );

         return default;
      }

      private async Task PingAsync(CancellationToken cancellationToken)
      {
         var ping = new PingMessage();
         ping.Nonce = this.randomNumberGenerator.GetUint64();

         await this.SendMessageAsync(ping).ConfigureAwait(false);

         this.status.PingSent(this.dateTimeProvider.GetTimeMicros(), ping);
         this.logger.LogDebug("Sent ping request with nonce {PingNonce}", this.status.PingRequestNonce);

         //in case of memory leak, investigate this.
         this.pingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

         // ensures the handshake is performed timely
         await this.DisconnectIfAsync(() =>
         {
            return new ValueTask<bool>(this.status.PingResponseTime == 0);
         }, TimeSpan.FromSeconds(TIMEOUT_INTERVAL), "Pong not received in time", this.pingCancellationTokenSource.Token).ConfigureAwait(false);
      }

      public async ValueTask<bool> ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
      {
         await this.SendMessageAsync(new PongMessage
         {
            PongFancyResponse = new PongFancyResponse
            {
               Nonce = message.Nonce,
               Quote = quoteService.GetRandomQuote()
            }
         }).ConfigureAwait(false);

         return true;
      }

      public ValueTask<bool> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
      {
         if (this.status.PingRequestNonce != 0 && message.PongFancyResponse?.Nonce == this.status.PingRequestNonce)
         {
            var (Nonce, RoundTrip) = this.status.PongReceived(this.dateTimeProvider.GetTimeMicros());
            this.logger.LogDebug("Received pong with nonce {PingNonce} in {PingRoundTrip} usec. {Quote}", Nonce, RoundTrip, message.PongFancyResponse.Quote);
            this.pingCancellationTokenSource.Cancel();
         }
         else
         {
            this.logger.LogDebug("Received pong with wrong nonce: {PingNonce}", this.status.PingRequestNonce);
         }

         return new ValueTask<bool>(true);
      }
   }
}