using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   /// <summary>
   /// Feed peers with data they require.
   /// </summary>
   /// <seealso cref="BaseProcessor" />
   public partial class AnnouncerProcessor : BaseProcessor,
      INetworkMessageHandler<SendHeadersMessage>,
      INetworkMessageHandler<SendCmpctMessage>
   {
      readonly IDateTimeProvider _dateTimeProvider;
      private readonly IInitialBlockDownloadTracker _ibdState;
      readonly ILocalServiceProvider _localServiceProvider;
      readonly IChainState _chainState;
      readonly BitcoinSettings _settings;

      public AnnouncerProcessor(ILogger<DataFeederProcessor> logger,
                                  IEventBus eventBus,
                                  IDateTimeProvider dateTimeProvider,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IInitialBlockDownloadTracker ibdState,
                                  ILocalServiceProvider localServiceProvider,
                                  IChainState chainState,
                                  IOptions<BitcoinSettings> options)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
      {
         _dateTimeProvider = dateTimeProvider;
         _ibdState = ibdState;
         _localServiceProvider = localServiceProvider;
         _chainState = chainState;
         _settings = options.Value;
      }

      protected override ValueTask OnPeerAttachedAsync()
      {
         // TODO: will attach to events needed to know when there is something useful to send to peers

         // e.g. RegisterLifeTimeEventHandler<BlockHeaderValidationSucceeded>(OnBlockHeaderValidationSucceededAsync);


         return base.OnPeerAttachedAsync();
      }


      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and update peer status based on its version message.
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override async ValueTask OnPeerHandshakedAsync()
      {
         HandshakeProcessor.HandshakeProcessorStatus handshakeStatus = PeerContext.Features.Get<HandshakeProcessor.HandshakeProcessorStatus>();

         VersionMessage peerVersion = handshakeStatus.PeerVersion!;

         await SendMessageAsync(minVersion: KnownVersion.V70012, new SendHeadersMessage()).ConfigureAwait(false);
      }

      /// <summary>
      /// The other peer prefer to be announced about new block using headers
      /// </summary>
      ValueTask<bool> INetworkMessageHandler<SendHeadersMessage>.ProcessMessageAsync(SendHeadersMessage message, CancellationToken cancellation)
      {
         _status.AnnounceNewBlockUsingSendHeaders = true;

         return new ValueTask<bool>(true);
      }


      /// <summary>
      /// The other peer prefer to receive blocks using cmpct messages.
      /// </summary>
      ValueTask<bool> INetworkMessageHandler<SendCmpctMessage>.ProcessMessageAsync(SendCmpctMessage message, CancellationToken cancellation)
      {
         if (message.Version == 1 || (_localServiceProvider.HasServices(NodeServices.Witness) && message.Version == 2))
         {
            if (!_status.ProvidesHeaderAndIDs)
            {
               _status.ProvidesHeaderAndIDs = true;
               _status.WantsCompactWitness = message.Version == 2;
            }

            // ignore later version announces
            if (_status.WantsCompactWitness = (message.Version == 2))
            {
               _status.AnnounceUsingCompactBlock = message.AnnounceUsingCompactBlock;
            }

            if (!_status.SupportsDesiredCompactVersion)
            {
               if (_localServiceProvider.HasServices(NodeServices.Witness))
               {
                  _status.SupportsDesiredCompactVersion = (message.Version == 2);
               }
               else
               {
                  _status.SupportsDesiredCompactVersion = (message.Version == 1);
               }
            }
         }
         else
         {
            logger.LogDebug("Ignoring sendcmpct message because its version is unknown.");
         }

         return new ValueTask<bool>(true);
      }

   }
}