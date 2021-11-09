using System.Collections.Generic;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation;

/// <summary>
/// Interface that allow to access to the list of <typeparamref name="TValidationRule"/> rules, automatically sorted by their configuration and dependency.
/// </summary>
/// <typeparam name="TValidationRule">The type of the validation rule.</typeparam>
public interface IValidationRuleSet<TValidationRule> where TValidationRule : class
{
   IEnumerable<TValidationRule> Rules { get; }

   /// <summary>
   /// Ensures rules are configured correctly, there aren't missing required rules and sort them based on requirements and explicit ordering.
   /// </summary>
   void SetupRules();
}
