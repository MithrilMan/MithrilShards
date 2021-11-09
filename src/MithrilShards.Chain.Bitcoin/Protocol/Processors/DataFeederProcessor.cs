using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

/// <summary>
/// Feed peers with data they require.
/// </summary>
/// <seealso cref="BaseProcessor" />
public partial class DataFeederProcessor : BaseProcessor,
   INetworkMessageHandler<GetHeadersMessage>,
   INetworkMessageHandler<GetBlocksMessage>
{
   readonly IDateTimeProvider _dateTimeProvider;
   private readonly IInitialBlockDownloadTracker _ibdState;
   readonly IChainState _chainState;
   readonly BitcoinSettings _settings;

   public DataFeederProcessor(ILogger<DataFeederProcessor> logger,
                               IEventBus eventBus,
                               IDateTimeProvider dateTimeProvider,
                               IPeerBehaviorManager peerBehaviorManager,
                               IInitialBlockDownloadTracker ibdState,
                               IChainState chainState,
                               IOptions<BitcoinSettings> options)
      : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
   {
      _dateTimeProvider = dateTimeProvider;
      _ibdState = ibdState;
      _chainState = chainState;
      _settings = options.Value;
   }

   protected override ValueTask OnPeerAttachedAsync()
   {
      // TODO: will attach to events needed to know when there is something useful to send to peers (or maybe create another processor for that? for example AnnouncerProcessor)

      // e.g. RegisterLifeTimeEventHandler<BlockHeaderValidationSucceeded>(OnBlockHeaderValidationSucceededAsync);


      return base.OnPeerAttachedAsync();
   }

   /// <summary>
   /// The peer wants some headers from us
   /// </summary>
   async ValueTask<bool> INetworkMessageHandler<GetHeadersMessage>.ProcessMessageAsync(GetHeadersMessage message, CancellationToken cancellation)
   {
      if (message is null) ThrowHelper.ThrowArgumentException(nameof(message));

      if (message.BlockLocator!.BlockLocatorHashes.Length > MAX_LOCATOR_SIZE)
      {
         logger.LogDebug("Exceeded maximum block locator size for getheaders message.");
         Misbehave(10, "Exceeded maximum getheaders block locator size", true);
         return true;
      }

      if (_ibdState.IsDownloadingBlocks() && !PeerContext.Permissions.Has(BitcoinPeerPermissions.DOWNLOAD))
      {
         logger.LogDebug("Ignoring getheaders from {PeerId} because node is in initial block download state.", PeerContext.PeerId);
         return true;
      }

      HeaderNode? startingNode;
      // If block locator is null, return the hashStop block
      if ((message.BlockLocator.BlockLocatorHashes?.Length ?? 0) == 0)
      {
         if (!_chainState.TryGetBestChainHeaderNode(message.HashStop!, out startingNode!))
         {
            logger.LogDebug("Empty block locator and HashStop not found");
            return true;
         }

         //TODO (ref net_processing.cpp 2479 tag 0.20)
         //if (!BlockRequestAllowed(pindex, chainparams.GetConsensus()))
         //{
         //   LogPrint(BCLog::NET, "%s: ignoring request from peer=%i for old block header that isn't in the main chain\n", __func__, pfrom->GetId());
         //   return true;
         //}
      }
      else
      {
         // Find the last block the caller has in the main chain
         startingNode = _chainState.FindForkInGlobalIndex(message.BlockLocator);
         _chainState.TryGetNext(startingNode, out startingNode);
      }

      logger.LogDebug("Serving headers from {StartingNodeHeight}:{StartingNodeHash}", startingNode?.Height, startingNode?.Hash);

      var headersToSend = new List<BlockHeader>();
      HeaderNode? headerToSend = startingNode;
      while (headerToSend != null)
      {
         if (!_chainState.TryGetBlockHeader(headerToSend, out BlockHeader? blockHeader))
         {
            //fatal error, should never happen
            ThrowHelper.ThrowNotSupportedException("Block Header not found");
            return true;
         }
         headersToSend.Add(blockHeader);
      }

      await SendMessageAsync(new HeadersMessage
      {
         Headers = headersToSend.ToArray()
      }).ConfigureAwait(false);

      return true;
   }

   ValueTask<bool> INetworkMessageHandler<GetBlocksMessage>.ProcessMessageAsync(GetBlocksMessage message, CancellationToken cancellation)
   {
      logger.LogWarning("TODO");

      return new ValueTask<bool>(true);
   }
}
