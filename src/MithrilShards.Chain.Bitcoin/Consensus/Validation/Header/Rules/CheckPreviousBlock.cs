using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Ensures the previous header is known and valid.
   /// </summary>
   /// <seealso cref="IHeaderValidationRule" />
   public class CheckPreviousBlock : IHeaderValidationRule
   {
      public const string PREV_BLOCK = "PREV_BLOCK";
      readonly ILogger<CheckPreviousBlock> logger;

      public CheckPreviousBlock(ILogger<CheckPreviousBlock> logger)
      {
         this.logger = logger;
      }

      public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         Protocol.Types.BlockHeader header = context.Header;

         if (header.PreviousBlockHash == null)
         {
            validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "prev-hash-null", "previous hash null, allowed only on genesis block");
            return false;
         }

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
