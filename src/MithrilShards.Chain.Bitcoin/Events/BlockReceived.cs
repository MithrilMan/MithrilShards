using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Events;

public class BlockReceived : EventBase
{
   /// <summary>
   /// Gets the received block.
   /// </summary>
   public Block ReceivedBlock { get; }

   /// <summary>
   /// Gets the peer that received the blocks, if applicable (blocks may be obtained by different sources not just by peers.
   /// </summary>
   public IPeerContext? Peer { get; }

   /// <summary>
   /// The fetcher that has obtained the block.
   /// May be null if the block has been received by a different type of source.
   /// </summary>
   /// <value>
   /// The fetcher.
   /// </value>
   public IBlockFetcher? Fetcher { get; }

   public BlockReceived(Block receivedBlock, IPeerContext? peer, IBlockFetcher? fetcher)
   {
      ReceivedBlock = receivedBlock;
      Peer = peer;
      Fetcher = fetcher;
   }
}
