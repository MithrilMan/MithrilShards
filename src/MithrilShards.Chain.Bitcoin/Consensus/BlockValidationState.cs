namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class BlockValidationState : ValidationState
   {
      public BlockValidationFailureContext FailureContext { get; private set; } = BlockValidationFailureContext.Unset;

      public void Invalid(BlockValidationFailureContext failureContext, string reason = "")
      {
         this.FailureContext = failureContext;
         this.Invalid(reason);
      }
   }
}