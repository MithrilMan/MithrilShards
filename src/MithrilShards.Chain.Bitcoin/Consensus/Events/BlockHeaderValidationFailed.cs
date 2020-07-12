﻿using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Consensus.Events
{
   /// <summary>
   /// A block header has been successfully validated.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.EventBus.EventBase" />
   public class BlockHeaderValidationFailed : EventBase
   {
      /// <summary>
      /// Gets the block header that failed the verification process.
      /// </summary>
      public BlockHeader FailedBlockHeader { get; }
      public BlockValidationState ValidationState { get; }

      /// <summary>
      /// The peer that sent us the header.
      /// If null, means the block header was issued by the node itself (e.g. during startup)
      /// </summary>
      public IPeerContext? PeerContext { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="BlockHeaderValidationFailed"/> class.
      /// </summary>
      /// <param name="failedBlockHeader">The block header that failed validation.</param>
      /// <param name="validationState">State of the validation.</param>
      /// <param name="peerContext">The peer context.</param>
      public BlockHeaderValidationFailed(BlockHeader failedBlockHeader, BlockValidationState validationState, IPeerContext? peerContext)
      {
         this.FailedBlockHeader = failedBlockHeader;
         this.ValidationState = validationState;
         this.PeerContext = peerContext;
      }
   }
}