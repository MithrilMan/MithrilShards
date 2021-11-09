using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Events;

/// <summary>
/// A block validation failed.
/// </summary>
/// <seealso cref="EventBase" />
public class BlockValidationFailed : EventBase
{
   /// <summary>
   /// Gets the block header that failed the verification process.
   /// </summary>
   public Block FailedBlock { get; }

   public BlockValidationState ValidationState { get; }

   /// <summary>
   /// The peer that sent us the header.
   /// If null, means the block header was issued by the node itself (e.g. during startup)
   /// </summary>
   public IPeerContext? PeerContext { get; }

   /// <summary>
   /// Initializes a new instance of the <see cref="BlockHeaderValidationFailed" /> class.
   /// </summary>
   /// <param name="failedBlock">The block that failed validation.</param>
   /// <param name="validationState">State of the validation.</param>
   /// <param name="peerContext">The peer context.</param>
   public BlockValidationFailed(Block failedBlock, BlockValidationState validationState, IPeerContext? peerContext)
   {
      FailedBlock = failedBlock;
      ValidationState = validationState;
      PeerContext = peerContext;
   }
}
