using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class PingPongProcessor : BaseProcessor,
      INetworkMessageHandler<PingMessage>,
      INetworkMessageHandler<PongMessage>
   {

      /// <summary>
      /// Time between pings automatically sent out for latency probing and keep-alive (in seconds).
      /// </summary>
      const int PING_INTERVAL = 2 * 60;

      /// <summary>
      /// Time after which to disconnect, after waiting for a ping response (in seconds).
      /// </summary>
      const int TIMEOUT_INTERVAL = 20 * 60;

      readonly IRandomNumberGenerator randomNumberGenerator;
      readonly IPeriodicWork periodicPing;

      public PingPongProcessor(ILogger<HandshakeProcessor> logger,
                               IEventBus eventBus,
                               IPeerBehaviorManager peerBehaviorManager,
                               IRandomNumberGenerator randomNumberGenerator,
                               IPeriodicWork periodicPing)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.randomNumberGenerator = randomNumberGenerator;
         this.periodicPing = periodicPing;
      }

      protected override ValueTask OnPeerHandshakedAsync()
      {
         _ = PingAsync(this.PeerContext.ConnectionCancellationTokenSource.Token);

         this.periodicPing.StartAsync(
            this.PeerContext.ConnectionCancellationTokenSource.Token,
            TimeSpan.FromSeconds(PING_INTERVAL),
            PingAsync
            );


         return default;
      }

      private async Task PingAsync(CancellationToken cancellationToken)
      {
         var ping = new PingMessage();
         if (PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V60001)
         {
            ping.Nonce = this.randomNumberGenerator.GetUint64();
         }

         await this.SendMessageAsync(ping).ConfigureAwait(false);

         this.status.PingSent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ping);
         this.logger.LogDebug("Sent ping request with nonce {PingNonce}", this.status.PingRequestNonce);


         // ensures the handshake is performed timely (supported only starting from version 60001)
         if (PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V60001)
         {
            await this.DisconnectIfAsync(() =>
            {
               return new ValueTask<bool>(this.status.PingResponseTime == 0);
            }, TimeSpan.FromSeconds(TIMEOUT_INTERVAL), "Pong not received in time", cancellationToken).ConfigureAwait(false);
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
      {
         await this.SendMessageAsync(new PongMessage { Nonce = message.Nonce }).ConfigureAwait(false);

         return true;
      }

      public ValueTask<bool> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
      {
         if (this.status.PingRequestNonce != 0 && message.Nonce == this.status.PingRequestNonce)
         {
            var (Nonce, RoundTrip) = this.status.PongReceived(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            this.logger.LogDebug("Received pong with nonce {PingNonce} in {PingRoundTrip} usec.", Nonce, RoundTrip);
         }
         else
         {
            this.logger.LogDebug("Received pong with wrong nonce: {PingNonce}", this.status.PingRequestNonce);
         }

         return new ValueTask<bool>(true);
      }
   }
}