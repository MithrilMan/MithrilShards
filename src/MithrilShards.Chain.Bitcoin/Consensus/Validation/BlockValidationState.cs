namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public class BlockValidationState : ValidationState
   {
      public BlockValidationFailureContext FailureContext { get; private set; } = BlockValidationFailureContext.Unset;

      public void Invalid(BlockValidationFailureContext failureContext, string reason, string debugMessage)
      {
         this.FailureContext = failureContext;
         base.Invalid(reason, debugMessage);
      }
   }
}