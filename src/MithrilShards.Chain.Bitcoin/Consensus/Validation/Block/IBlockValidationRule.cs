namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block
{
   public interface IBlockValidationRule
   {
      /// <summary>
      /// Checks the specified rule.
      /// </summary>
      /// <param name="context">The rule context.</param>
      /// <param name="validationState">State of the validation.</param>
      /// <returns>
      ///   <see langword="true" /> if the rule is not violated, <see langword="false" /> if the rule has been violated.
      /// </returns>
      bool Check(IBlockValidationContext context, ref BlockValidationState validationState);
   }
}
