namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public class BlockValidationState : ValidationState
   {
      public int FailureContext { get; private set; } = BlockValidationFailureContext.Unset;

      public void Invalid(int failureContext, string reason, string debugMessage)
      {
         this.FailureContext = failureContext;
         base.Invalid(reason, debugMessage);
      }
   }
}