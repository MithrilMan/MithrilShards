using System.Collections.Generic;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public interface IValidationRulesChecker
   {
      /// <summary>
      /// Verifies that all registered validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <typeparam name="TValidationRule">The type of the validation rules.</typeparam>
      /// <param name="rules">The rules to verify.</param>
      void VerifyValidationRules<TValidationRule>(IEnumerable<TValidationRule> rules);
   }
}