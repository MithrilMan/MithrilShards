using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules
{
   public abstract class HeaderValidationRuleBase : IHeaderValidationRule
   {
      protected readonly ILogger logger;

      public HeaderValidationRuleBase(ILogger logger)
      {
         this.logger = logger;
      }

      public abstract void Check(IHeaderValidationContext context);
   }
}
