using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules;

/// <summary>
/// Ensures the previous header is known and valid.
/// </summary>
/// <seealso cref="IHeaderValidationRule" />
public class CheckPreviousBlock : IHeaderValidationRule
{
   public const string PREV_BLOCK = "PREV_BLOCK";
   readonly ILogger<CheckPreviousBlock> _logger;

   public CheckPreviousBlock(ILogger<CheckPreviousBlock> logger)
   {
      _logger = logger;
   }

   public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
   {
      Protocol.Types.BlockHeader header = context.Header;

      if (header.PreviousBlockHash == null)
      {
         validationState.Invalid(BlockValidationStateResults.InvalidHeader, "prev-hash-null", "previous hash null, allowed only on genesis block");
         return false;
      }

      // ensures tip previous header is present.
      if (!context.ChainState.TryGetKnownHeaderNode(context.Header.PreviousBlockHash!, out HeaderNode? previousNode))
      {
         //previous tip header not found, abort.
         validationState.Invalid(BlockValidationStateResults.MissingPreviousHeader, "prev-blk-not-found", "previous header not found, can't connect headers");
         return false;
      }

      if (previousNode.IsInvalid())
      {
         validationState.Invalid(BlockValidationStateResults.CachedInvalid, "bad-prevblk", "previous block invalid");
         return false;
      }

      context.SetData(PREV_BLOCK, previousNode);

      return true;
   }
}
