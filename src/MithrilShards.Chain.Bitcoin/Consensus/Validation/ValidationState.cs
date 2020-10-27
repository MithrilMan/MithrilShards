namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Base class for capturing information about block/transaction validation.
   /// This is sub classed by TxValidationState and BlockValidationState for validation information on transactions and blocks respectively
   /// </summary>
   public abstract class ValidationState<TValidationResult> where TValidationResult : struct, System.Enum
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

      private InnerState mode = InnerState.Valid;

      public TValidationResult Result { get; private set; }

      public string? RejectReason { get; private set; }

      public string? DebugMessage { get; private set; }

      public bool IsValid() => this.mode == InnerState.Valid;

      public bool IsInvalid() => this.mode == InnerState.Invalid;

      public bool IsError() => this.mode == InnerState.Error;

      public bool Invalid(TValidationResult result, string reason, string? debugMessage = null)
      {
         this.Result = result;
         this.RejectReason = reason;
         this.DebugMessage = debugMessage;
         this.mode = InnerState.Invalid;

         return false;
      }

      public bool Error(string reason)
      {
         if (this.mode == InnerState.Valid)
         {
            this.RejectReason = reason;
         }

         this.mode = InnerState.Error;

         return false;
      }


      public override string ToString()
      {
         if (this.IsValid())
         {
            return "Valid";
         }
         else
         {
            return $"{this.RejectReason ?? string.Empty} ({this.DebugMessage ?? string.Empty})";
         }
      }
   }
}
