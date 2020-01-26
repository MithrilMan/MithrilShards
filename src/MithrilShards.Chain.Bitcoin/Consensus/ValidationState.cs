namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Base class for capturing information about block/transaction validation.
   /// This is sub classed by TxValidationState and BlockValidationState for validation information on transactions and blocks respectively
   /// </summary>
   public abstract class ValidationState
   {
      public enum InnerState
      {
         /// <summary>
         /// Everything is ok
         /// </summary>
         Valid,

         /// <summary>
         /// Network rule violation (DoS value may be set)
         /// </summary>
         Invalid,

         /// <summary>
         /// Runtime error
         /// </summary>
         Error
      }

      private InnerState mode;


      public string? RejectReason { get; private set; }
      public bool IsValid() => this.mode == InnerState.Valid;
      public bool IsInvalid() => this.mode == InnerState.Invalid;
      public bool IsError() => this.mode == InnerState.Error;

      protected void Invalid(string reason)
      {
         this.RejectReason = reason;
      }

      public void Error(string reason)
      {
         if (this.mode == InnerState.Valid)
         {
            this.RejectReason = reason;
         }

         this.mode = InnerState.Error;
      }
   }
}
