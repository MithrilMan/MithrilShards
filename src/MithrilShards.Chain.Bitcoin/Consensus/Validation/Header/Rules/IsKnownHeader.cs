using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header and if it is, ensures it's not invalid and return the known validated headers, forcing validation to conclude.
   /// If the header is known and was previously flagged as invalid, returns a failure.
   /// </summary>
   /// <seealso cref="IHeaderValidationRule" />
   [RulePrecedence(preferredExecutionOrder: 0)]
   public class IsKnownHeader : IHeaderValidationRule
   {
      readonly ILogger<IsKnownHeader> logger;

      public IsKnownHeader(ILogger<IsKnownHeader> logger)
      {
         this.logger = logger;
      }

      public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         HeaderNode? existingHeader = context.KnownHeader;

         // check if the tip we want to set is already into our chain
         if (existingHeader != null)
         {
            if (existingHeader.IsInvalid())
            {
               validationState.Invalid(BlockValidationStateResults.CachedInvalid, "duplicate", "block marked as invalid");
               return false;
            }

            // if the header has been already validated before, we can skip the validation process.
            context.ForceAsValid("The header we want to accept is already in our headers chain.");
         }

         return true;
      }
   }
}
