using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public abstract class HeaderValidationRuleBase : IHeaderValidationRule
   {
      protected readonly ILogger logger;

      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderValidationRuleBase"/> class.
      /// </summary>
      /// <param name="logger">The logger.</param>
      public HeaderValidationRuleBase(ILogger logger)
      {
         this.logger = logger;
      }

      /// <summary>
      /// Checks the specified rule.
      /// </summary>
      /// <param name="context">The rule context.</param>
      /// <param name="validationState">State of the validation.</param>
      /// <returns>
      ///   <see langword="true" /> if the rule is not violated, <see langword="false" /> if the rule has been violated.
      /// </returns>
      public abstract bool Check(IHeaderValidationContext context, ref BlockValidationState validationState);
   }
}
