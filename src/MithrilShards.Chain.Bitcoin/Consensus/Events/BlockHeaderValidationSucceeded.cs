using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Consensus.Events
{
   /// <summary>
   /// A block header batch has succeeded.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
   public class BlockHeaderValidationSucceeded : EventBase
   {
      /// <summary>
      /// Gets the number of headers validated in the batch.
      /// </summary>
      public int ValidatedHeadersCount { get; }

      /// <summary>
      /// The last validated block header.
      /// </summary>
      public BlockHeader LastValidatedBlockHeader { get; }

      /// <summary>
      /// The header node corresponding to the last validated block header.
      /// </summary>
      public HeaderNode LastValidatedHeaderNode { get; }
      public bool NewHeaderFound { get; }

      /// <summary>
      /// The peer that sent us the header.
      /// If null, means the block header was issued by the node itself (e.g. during startup)
      /// </summary>
      public IPeerContext? PeerContext { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockHeaderValidationSucceeded"/> class.
      /// </summary>
      /// <param name="validatedHeaders">The number of headers validated in the batch.</param>
      /// <param name="lastValidatedBlockHeader">The last validated block header.</param>
      /// <param name="lastValidatedHeaderNode">The last validated header node.</param>
      /// <param name="peerContext">The peer context.</param>
      public BlockHeaderValidationSucceeded(int validatedHeaders,
                                            BlockHeader lastValidatedBlockHeader,
                                            HeaderNode lastValidatedHeaderNode,
                                            bool newHeaderFound,
                                            IPeerContext? peerContext)
      {
         this.ValidatedHeadersCount = validatedHeaders;
         this.LastValidatedBlockHeader = lastValidatedBlockHeader;
         this.LastValidatedHeaderNode = lastValidatedHeaderNode;
         this.NewHeaderFound = newHeaderFound;
         this.PeerContext = peerContext;
      }
   }
}
