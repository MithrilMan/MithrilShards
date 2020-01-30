using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.ValidationRules;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusValidator
   {
      /// <summary>
      /// The logger
      /// </summary>
      readonly ILogger<ConsensusValidator> logger;

      /// <summary>
      /// The event bus
      /// </summary>
      readonly IEventBus eventBus;

      /// <summary>
      /// The known header validation rules.
      /// </summary>
      readonly IEnumerable<IHeaderValidationRule> headerValidationRules;

      public ConsensusValidator(ILogger<ConsensusValidator> logger, IEventBus eventBus, IEnumerable<IHeaderValidationRule> headerValidationRules)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.headerValidationRules = headerValidationRules;

         this.VerifyValidationRules(this.headerValidationRules);
      }

      /// <summary>
      /// Verifies that all registered header validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <exception cref="NotImplementedException"></exception>
      private void VerifyValidationRules<TValidationRuleContext>(IEnumerable<TValidationRuleContext> rules)
      {
         foreach (TValidationRuleContext rule in rules)
         {
            Type ruleType = rule!.GetType();
            foreach (Type requiredRule in this.GetRequiredRules(ruleType))
            {
               if (!typeof(TValidationRuleContext).IsAssignableFrom(requiredRule))
               {
                  throw new ArgumentException($"{nameof(ruleType)} must implement {typeof(TValidationRuleContext).Name}.");
               }

               if (!rules.Any(rule => requiredRule.IsAssignableFrom(requiredRule)))
               {
                  throw new ArgumentException($"{nameof(ruleType)} requires '{requiredRule.Name}' but the rule (or a subclass of that rule) is not registered.");
               }
            }
         }
      }

      private List<Type> GetRequiredRules(Type fromType)
      {
         return fromType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
            .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
            .ToList();
      }
   }
}
