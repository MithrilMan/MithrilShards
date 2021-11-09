using System;
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

namespace MithrilShards.Example.Protocol.Processors;

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
   /// It has to be lower than PING_INTERVAL
   /// </summary>
   const int TIMEOUT_INTERVAL = 5;

   readonly IRandomNumberGenerator _randomNumberGenerator;
   readonly IDateTimeProvider _dateTimeProvider;
   readonly IPeriodicWork _periodicPing;
   readonly IQuoteService _quoteService;
   private CancellationTokenSource _pingCancellationTokenSource = null!;

   public PingPongProcessor(ILogger<PingPongProcessor> logger,
                            IEventBus eventBus,
                            IPeerBehaviorManager peerBehaviorManager,
                            IRandomNumberGenerator randomNumberGenerator,
                            IDateTimeProvider dateTimeProvider,
                            IPeriodicWork periodicPing,
                            IQuoteService quoteService)
      : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
   {
      _randomNumberGenerator = randomNumberGenerator;
      _dateTimeProvider = dateTimeProvider;
      _periodicPing = periodicPing;
      _quoteService = quoteService;
   }

   protected override ValueTask OnPeerHandshakedAsync()
   {
      _ = _periodicPing.StartAsync(
            label: $"{nameof(_periodicPing)}-{PeerContext.PeerId}",
            work: PingAsync,
            interval: TimeSpan.FromSeconds(PING_INTERVAL),
            cancellation: PeerContext.ConnectionCancellationTokenSource.Token
         );

      return default;
   }

   private async Task PingAsync(CancellationToken cancellationToken)
   {
      var ping = new PingMessage
      {
         Nonce = _randomNumberGenerator.GetUint64()
      };

      await SendMessageAsync(ping).ConfigureAwait(false);

      _status.PingSent(_dateTimeProvider.GetTimeMicros(), ping);
      logger.LogDebug("Sent ping request with nonce {PingNonce}", _status.PingRequestNonce);

      //in case of memory leak, investigate this.
      _pingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

      // ensures the handshake is performed timely
      await DisconnectIfAsync(() =>
      {
         return new ValueTask<bool>(_status.PingResponseTime == 0);
      }, TimeSpan.FromSeconds(TIMEOUT_INTERVAL), "Pong not received in time", _pingCancellationTokenSource.Token).ConfigureAwait(false);
   }

   async ValueTask<bool> INetworkMessageHandler<PingMessage>.ProcessMessageAsync(PingMessage message, CancellationToken cancellation)
   {
      await SendMessageAsync(new PongMessage
      {
         PongFancyResponse = new PongFancyResponse
         {
            Nonce = message.Nonce,
            Quote = _quoteService.GetRandomQuote()
         }
      }).ConfigureAwait(false);

      return true;
   }

   ValueTask<bool> INetworkMessageHandler<PongMessage>.ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
   {
      if (_status.PingRequestNonce != 0 && message.PongFancyResponse?.Nonce == _status.PingRequestNonce)
      {
         (ulong Nonce, long RoundTrip) = _status.PongReceived(_dateTimeProvider.GetTimeMicros());
         logger.LogDebug("Received pong with nonce {PingNonce} in {PingRoundTrip} usec. {Quote}", Nonce, RoundTrip, message.PongFancyResponse.Quote);
         _pingCancellationTokenSource.Cancel();
      }
      else
      {
         logger.LogDebug("Received pong with wrong nonce: {PingNonce}", _status.PingRequestNonce);
      }

      return new ValueTask<bool>(true);
   }
}
