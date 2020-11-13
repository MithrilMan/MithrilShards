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

      private CancellationTokenSource pingCancellationTokenSource = null!;

      private string[] quotes =
      {
         "There is only one Lord of the Ring, only one who can bend it to his will. And he does not share power.",
         "That there’s some good in this world, Mr. Frodo… and it’s worth fighting for.",
         "Even the smallest person can change the course of the future.",
         "The time of the Elves… is over. Do we leave Middle-Earth to its fate? Do we let them stand alone?",
         "We swears, to serve the master of the Precious. We will swear on… on the Precious!",
         "I am Gandalf the White. And I come back to you now… at the turn of the tide.",
         "Oh, it’s quite simple. If you are a friend, you speak the password, and the doors will open.",
         "Well, what can I tell you? Life in the wide world goes on much as it has this past Age, full of its own comings and goings, scarcely aware of the existence of Hobbits, for which I am very thankful.",
         "For the time will soon come when Hobbits will shape the fortunes of all.",
         "There is no curse in Elvish, Entish, or the tongues of Men for this treachery.",
         "I would rather share one lifetime with you than face all the Ages of this world alone.",
         "A day may come when the courage of men fails… but it is not THIS day.",
         "The Ring has awoken, it’s heard its masters call.",
         "Your time will come. You will face the same Evil, and you will defeat it.",
         "The board is set, the pieces are moving. We come to it at last, the great battle of our time.",
         "But the fat Hobbit, he knows. Eyes always watching.",
         "Mordor. The one place in Middle-Earth we don’t want to see any closer. And it’s the one place we’re trying to get to. It’s just where we can’t get. Let’s face it, Mr. Frodo, we’re lost.",
         "I thought up an ending for my book. ‘And he lives happily ever after, till the end of his days.",
         "You are the luckiest, the canniest, and the most reckless man I ever knew. Bless you, laddie.",
         "I’m glad to be with you, Samwise Gamgee…here at the end of all things.",
      };

      public PingPongProcessor(ILogger<PingPongProcessor> logger,
                               IEventBus eventBus,
                               IPeerBehaviorManager peerBehaviorManager,
                               IRandomNumberGenerator randomNumberGenerator,
                               IDateTimeProvider dateTimeProvider,
                               IPeriodicWork periodicPing)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.randomNumberGenerator = randomNumberGenerator;
         this.dateTimeProvider = dateTimeProvider;
         this.periodicPing = periodicPing;
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
               Quote = quotes[randomNumberGenerator.GetUint32() % quotes.Length]
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