using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Ensures that rules are configured correctly
   /// </summary>
   public class ValidationRulesChecker : IValidationRulesChecker
   {
      readonly ILogger<ValidationRulesChecker> logger;

      public ValidationRulesChecker(ILogger<ValidationRulesChecker> logger)
      {
         this.logger = logger;
      }

      /// <summary>
      /// Verifies that all registered validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <typeparam name="TValidationRule">The type of the validation rules.</typeparam>
      /// <param name="rules">The rules to verify.</param>
      public void VerifyValidationRules<TValidationRule>(IEnumerable<TValidationRule> rules)
      {
         // ensures that registered rules dependencies are honored
         List<Type> GetRequiredRules(Type ruleType)
         {
            return ruleType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
               .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
               .ToList();
         }

         Type validationRulesType = typeof(TValidationRule);

         using IDisposable logScope = logger.BeginScope("Verifying validation rules for {ValidationRuleType}", validationRulesType.Name);
         foreach (TValidationRule rule in rules)
         {
            Type ruleType = rule!.GetType();
            foreach (Type requiredRule in GetRequiredRules(ruleType))
            {
               if (!validationRulesType.IsAssignableFrom(requiredRule))
               {
                  ThrowHelper.ThrowArgumentException($"{nameof(ruleType)} must implement {validationRulesType.Name}.");
               }

               if (!rules.Any(rule => requiredRule.IsAssignableFrom(requiredRule)))
               {
                  ThrowHelper.ThrowArgumentException($"{nameof(ruleType)} requires '{requiredRule.Name}' but the rule (or a subclass of that rule) is not registered.");
               }
            }
         }
      }

      /// <summary>
      /// Gets the required rules defined using <see cref="RequiresRuleAttribute" /> for the rule of <paramref name="ruleType" /> type.
      /// </summary>
      /// <param name="ruleType">Type of the rule that need to get required rules.</param>
      /// <returns></returns>
      private static List<Type> GetRequiredRules(Type ruleType)
      {
         return ruleType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
            .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
            .ToList();
      }


   }
}
