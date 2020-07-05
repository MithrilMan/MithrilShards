﻿using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.HeaderValidationRuleBase" />
   public class CheckPreviousBlock : HeaderValidationRuleBase
   {
      public const string PREV_BLOCK = "PREV_BLOCK";

      public CheckPreviousBlock(ILogger<CheckPreviousBlock> logger) : base(logger) { }

      public override bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         // ensures tip previous header is present.
         if (!context.ChainState.TryGetKnownHeaderNode(context.Header.PreviousBlockHash!, out HeaderNode? previousNode))
         {
            //previous tip header not found, abort.
            validationState.Invalid(BlockValidationFailureContext.BlockMissingPreviousHeader, "prev-blk-not-found", "previous header not found, can't connect headers");
            return false;
         }

         if (previousNode.IsInvalid())
         {
            validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "bad-prevblk", "previous block invalid");
            return false;
         }

         context.SetData(PREV_BLOCK, previousNode);

         return true;
      }
   }
}
