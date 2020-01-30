using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusValidator : IConsensusValidator
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
      readonly IHeaderValidationContextFactory headerValidationContextFactory;

      private readonly object headerValidationLock = new object();

      public ConsensusValidator(ILogger<ConsensusValidator> logger,
                                IEventBus eventBus,
                                IEnumerable<IHeaderValidationRule> headerValidationRules,
                                IHeaderValidationContextFactory headerValidationContextFactory)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.headerValidationRules = headerValidationRules;
         this.headerValidationContextFactory = headerValidationContextFactory;

         this.VerifyValidationRules(this.headerValidationRules);
      }

      /// <summary>
      /// Verifies that all registered validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <typeparam name="TValidationRule">The type of the validation rules.</typeparam>
      /// <param name="rules">The rules to verify.</param>
      private void VerifyValidationRules<TValidationRule>(IEnumerable<TValidationRule> rules)
      {
         Type validationRulesType = typeof(TValidationRule);

         using IDisposable logScope = this.logger.BeginScope("Verifying validation rules for {ValidationRuleType}", validationRulesType.Name);
         foreach (TValidationRule rule in rules)
         {
            Type ruleType = rule!.GetType();
            foreach (Type requiredRule in this.GetRequiredRules(ruleType))
            {
               if (!validationRulesType.IsAssignableFrom(requiredRule))
               {
                  throw new ArgumentException($"{nameof(ruleType)} must implement {validationRulesType.Name}.");
               }

               if (!rules.Any(rule => requiredRule.IsAssignableFrom(requiredRule)))
               {
                  throw new ArgumentException($"{nameof(ruleType)} requires '{requiredRule.Name}' but the rule (or a subclass of that rule) is not registered.");
               }
            }
         }
      }

      /// <summary>
      /// Gets the required rules defined using <see cref="RequiresRuleAttribute" /> for the rule of <paramref name="ruleType" /> type.
      /// </summary>
      /// <param name="ruleType">Type of the rule that need to get required rules.</param>
      /// <returns></returns>
      private List<Type> GetRequiredRules(Type ruleType)
      {
         return ruleType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
            .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
            .ToList();
      }

      /// <summary>
      /// Validates the header performing checks for every <see cref="IHeaderValidationRule"/> known rule.
      /// </summary>
      /// <param name="header">The header to validate.</param>
      /// <param name="validationState">The resulting state of the validation.</param>
      /// <returns>
      /// <see langword="true"/> if the validation succeed, <see langword="false"/> otherwise and the reason of the fault
      /// can be found in <paramref name="validationState"/>.
      /// </returns>
      public bool ValidateHeader(BlockHeader header, out BlockValidationState validationState)
      {
         lock (this.headerValidationLock)
         {
            IHeaderValidationContext context = this.headerValidationContextFactory.Create(header);

            validationState = new BlockValidationState();
            foreach (IHeaderValidationRule rule in this.headerValidationRules)
            {
               if (!rule.Check(context, ref validationState))
               {
                  this.logger.LogDebug("Header validation failed: {HeaderValidationState}", validationState.ToString());
                  return false;
               }
            }

            validationState.IsValid();
            return true;
         }
      }
   }
}
