using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Events
{
   /// <summary>
   /// A block header batch has succeeded.
   /// </summary>
   /// <seealso cref="EventBase" />
   public class BlockValidationSucceeded : EventBase
   {
      /// <summary>
      /// The last validated block header.
      /// </summary>
      public Block ValidatedBlock { get; }

      /// <summary>
      /// Gets a value indicating whether this validated block is a new block.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance is new block; otherwise, <c>false</c>.
      /// </value>
      public bool IsNewBlock { get; }

      /// <summary>
      /// The peer that sent us the header.
      /// If null, means the block header was issued by the node itself (e.g. during startup)
      /// </summary>
      public IPeerContext? PeerContext { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockHeaderValidationSucceeded" /> class.
      /// </summary>
      /// <param name="validatedBlock">The validated block.</param>
      /// <param name="peerContext">The peer context.</param>
      public BlockValidationSucceeded(Block validatedBlock, bool isNewBlock, IPeerContext? peerContext)
      {
         ValidatedBlock = validatedBlock;
         IsNewBlock = isNewBlock;
         PeerContext = peerContext;
      }
   }
}