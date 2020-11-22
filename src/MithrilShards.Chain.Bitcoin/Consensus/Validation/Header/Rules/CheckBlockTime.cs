using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header
   /// </summary>
   /// <seealso cref="IHeaderValidationRule" />
   [RequiresRule(typeof(CheckPreviousBlock))]
   public class CheckBlockTime : IHeaderValidationRule
   {
      /// <summary>
      /// Maximum amount of time that a block timestamp is allowed to exceed the
      /// current network-adjusted time before the block will be accepted.
      /// </summary>
      const uint MAX_FUTURE_BLOCK_TIME = 2 * 60 * 60;
      readonly ILogger<CheckPreviousBlock> _logger;
      readonly IHeaderMedianTimeCalculator _headerMedianTimeCalculator;
      readonly IDateTimeProvider _dateTimeProvider;

      public CheckBlockTime(ILogger<CheckPreviousBlock> logger,
                            IHeaderMedianTimeCalculator headerMedianTimeCalculator,
                            IDateTimeProvider dateTimeProvider)
      {
         _logger = logger;
         _headerMedianTimeCalculator = headerMedianTimeCalculator;
         _dateTimeProvider = dateTimeProvider;
      }

      public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         if (!context.TryGetData(CheckPreviousBlock.PREV_BLOCK, out HeaderNode? previousHeaderNode))
         {
            ThrowHelper.ThrowArgumentException("Fatal exception, this rule must be executed before CheckPreviousBlock, need previous header node");
            return false;
         }

         if (context.Header.TimeStamp <= _headerMedianTimeCalculator.Calculate(previousHeaderNode!.Hash, previousHeaderNode.Height))
         {
            validationState.Invalid(BlockValidationStateResults.InvalidHeader, "time-too-old", "block's timestamp is too early");
            return false;
         }

         // Check timestamp.
         if (context.Header.TimeStamp > (_dateTimeProvider.GetAdjustedTimeAsUnixTimestamp() + MAX_FUTURE_BLOCK_TIME))
         {
            validationState.Invalid(BlockValidationStateResults.TimeFuture, "time-too-new", "block timestamp too far in the future");
            return false;
         }

         return true;
      }
   }
}
