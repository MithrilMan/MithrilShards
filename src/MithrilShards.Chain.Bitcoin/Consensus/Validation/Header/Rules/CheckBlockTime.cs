using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.HeaderValidationRuleBase" />
   public class CheckBlockTime : HeaderValidationRuleBase
   {
      /// <summary>
      /// Maximum amount of time that a block timestamp is allowed to exceed the
      /// current network-adjusted time before the block will be accepted.
      /// </summary>
      const uint MAX_FUTURE_BLOCK_TIME = 2 * 60 * 60;

      readonly IHeaderMedianTimeCalculator headerMedianTimeCalculator;
      readonly IDateTimeProvider dateTimeProvider;

      public CheckBlockTime(ILogger<CheckPreviousBlock> logger,
                            IHeaderMedianTimeCalculator headerMedianTimeCalculator,
                            IDateTimeProvider dateTimeProvider) : base(logger)
      {
         this.headerMedianTimeCalculator = headerMedianTimeCalculator;
         this.dateTimeProvider = dateTimeProvider;
      }

      public override bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         if (!context.TryGetData(CheckPreviousBlock.PREV_BLOCK, out HeaderNode? previousHeaderNode))
         {
            ThrowHelper.ThrowArgumentException("Fatal exception, this rule must be executed before CheckPreviousBlock, need previous header node");
            return false;
         }

         if (context.Header.TimeStamp <= this.headerMedianTimeCalculator.Calculate(previousHeaderNode!.Hash, previousHeaderNode.Height))
         {
            validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "time-too-old", "block's timestamp is too early");
            return false;
         }

         // Check timestamp.
         if (context.Header.TimeStamp > (this.dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() + MAX_FUTURE_BLOCK_TIME))
         {
            validationState.Invalid(BlockValidationFailureContext.BlockTimeFuture, "time-too-new", "block timestamp too far in the future");
            return false;
         }

         return true;
      }
   }
}
