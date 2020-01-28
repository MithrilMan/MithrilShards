using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus.Events
{
   /// <summary>
   /// A block header has been successfully added to the repository
   /// </summary>
   /// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
   public class BlockHeaderAddedToRepository : EventBase
   {
      public BlockHeader BlockHeader { get; }

      public BlockHeaderAddedToRepository(BlockHeader blockHeader)
      {
         this.BlockHeader = blockHeader;
      }
   }
}
