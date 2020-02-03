using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.HeaderValidationRuleBase" />
   public class CheckPreviousBlock : HeaderValidationRuleBase
   {
      public CheckPreviousBlock(ILogger<CheckPreviousBlock> logger) : base(logger) { }

      public override bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         // ensures tip previous header is present.
         if (!context.HeadersTree.TryGetNode(context.Header.PreviousBlockHash!, false, out HeaderNode? previousNode))
         {
            //previous tip header not found, abort.
            validationState.Invalid(BlockValidationFailureContext.BlockMissingPreviousHeader, "prev-blk-not-found", "previous header not found, can't connect headers");
            return false;
         }

         if (previousNode.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
         {
            validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "bad-prevblk", "previous block invalid");
            return false;
         }

         return true;
      }
   }
}
