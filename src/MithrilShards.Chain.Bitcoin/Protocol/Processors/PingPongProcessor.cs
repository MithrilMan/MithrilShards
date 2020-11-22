﻿using System;
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

      readonly IRandomNumberGenerator _randomNumberGenerator;
      readonly IDateTimeProvider _dateTimeProvider;
      readonly IPeriodicWork _periodicPing;

      private CancellationTokenSource _pingCancellationTokenSource = null!;

      public PingPongProcessor(ILogger<PingPongProcessor> logger,
                               IEventBus eventBus,
                               IPeerBehaviorManager peerBehaviorManager,
                               IRandomNumberGenerator randomNumberGenerator,
                               IDateTimeProvider dateTimeProvider,
                               IPeriodicWork periodicPing)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
      {
         this._randomNumberGenerator = randomNumberGenerator;
         this._dateTimeProvider = dateTimeProvider;
         this._periodicPing = periodicPing;
      }

      protected override ValueTask OnPeerHandshakedAsync()
      {
         _ = this._periodicPing.StartAsync(
               label: $"{nameof(_periodicPing)}-{PeerContext.PeerId}",
               work: PingAsync,
               interval: TimeSpan.FromSeconds(PING_INTERVAL),
               cancellation: PeerContext.ConnectionCancellationTokenSource.Token
            );

         return default;
      }

      private async Task PingAsync(CancellationToken cancellationToken)
      {
         var ping = new PingMessage();
         if (PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V60001)
         {
            ping.Nonce = this._randomNumberGenerator.GetUint64();
         }

         await this.SendMessageAsync(ping).ConfigureAwait(false);

         this._status.PingSent(this._dateTimeProvider.GetTimeMicros(), ping);
         this.logger.LogDebug("Sent ping request with nonce {PingNonce}", this._status.PingRequestNonce);

         //in case of memory leak, investigate this.
         this._pingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

         // ensures the handshake is performed timely (supported only starting from version 60001)
         if (PeerContext.NegotiatedProtocolVersion.Version >= KnownVersion.V60001)
         {
            await this.DisconnectIfAsync(() =>
            {
               return new ValueTask<bool>(this._status.PingResponseTime == 0);
            }, TimeSpan.FromSeconds(TIMEOUT_INTERVAL), "Pong not received in time", this._pingCancellationTokenSource.Token).ConfigureAwait(false);
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
      {
         await this.SendMessageAsync(new PongMessage { Nonce = message.Nonce }).ConfigureAwait(false);

         return true;
      }

      public ValueTask<bool> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
      {
         if (this._status.PingRequestNonce != 0 && message.Nonce == this._status.PingRequestNonce)
         {
            (ulong Nonce, long RoundTrip) = this._status.PongReceived(this._dateTimeProvider.GetTimeMicros());
            this.logger.LogDebug("Received pong with nonce {PingNonce} in {PingRoundTrip} usec.", Nonce, RoundTrip);
            this._pingCancellationTokenSource.Cancel();
         }
         else
         {
            this.logger.LogDebug("Received pong with wrong nonce: {PingNonce}", this._status.PingRequestNonce);
         }

         return new ValueTask<bool>(true);
      }
   }
}