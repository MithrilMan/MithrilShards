namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules
{
   public interface IHeaderValidationRule
   {
      void Check(IHeaderValidationContext context);
   }
}
