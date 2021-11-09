using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Events;

/// <summary>
/// A block header has been successfully added to the repository
/// </summary>
/// <seealso cref="EventBase" />
public class BlockHeaderAddedToRepository : EventBase
{
   public BlockHeader BlockHeader { get; }

   public BlockHeaderAddedToRepository(BlockHeader blockHeader)
   {
      BlockHeader = blockHeader;
   }
}
