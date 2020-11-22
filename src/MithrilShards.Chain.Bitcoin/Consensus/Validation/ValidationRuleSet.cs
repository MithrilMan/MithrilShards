using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.DataAlgorithms;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public class ValidationRuleSet<TValidationRule> : IValidationRuleSet<TValidationRule> where TValidationRule : class
   {
      protected class RuleDefinition
      {
         public TValidationRule Rule { get; }

         public uint PreferredExecutionOrder { get; }

         private readonly HashSet<TValidationRule> executeAfter = new HashSet<TValidationRule>();

         public IEnumerable<TValidationRule> GetDependencies() => executeAfter;

         public RuleDefinition(TValidationRule rule, uint preferredExecutionOrder)
         {
            this.Rule = rule;
            this.PreferredExecutionOrder = preferredExecutionOrder;
         }

         public void ExecuteAfter(TValidationRule rule)
         {
            this.executeAfter.Add(rule);
         }
      }

      protected readonly ILogger<ValidationRuleSet<TValidationRule>> logger;
      protected List<TValidationRule> rules;

      public IEnumerable<TValidationRule> Rules => this.rules;

      public ValidationRuleSet(ILogger<ValidationRuleSet<TValidationRule>> logger, IEnumerable<TValidationRule> rules)
      {
         this.logger = logger;
         this.rules = rules.ToList();
      }

      public void SetupRules()
      {
         using IDisposable logScope = logger.BeginScope("Setting up validation rules for {ValidationRuleType}", typeof(TValidationRule));

         List<RuleDefinition> definitions = this.VerifyValidationRules();

         // definitions now contains a list of rule definitions with all data needed to sort them
         // use topological sorting to solve dependency graph

         var resolver = new TopologicalSorter<RuleDefinition>();
         foreach (RuleDefinition definition in definitions)
         {
            resolver.Add(
               item: definition,
               dependencies: definition.GetDependencies()
                  .Select(dependency => definitions.FirstOrDefault(definition => definition == dependency))
                  .Where(dependency => dependency != null) // ignore missing rules. Proper check on mandatory rules has been performed in VerifyValidationRules
               );
         }

         (IEnumerable<(ValidationRuleSet<TValidationRule>.RuleDefinition item, int level)> sorted, IEnumerable<ValidationRuleSet<TValidationRule>.RuleDefinition> cycled) = resolver.Sort();
         if (cycled.Count() > 0)
         {
            string circularDependency = String.Join(", ", cycled.Select(definition => definition.Rule.GetType().Name));
            ThrowHelper.ThrowNotSupportedException($"Error configuring {typeof(TValidationRule).Name} rules, circular dependency detected: {circularDependency}");
         }

         this.rules = sorted
            .OrderBy(definition => definition.level)
            .ThenBy(definition => definition.item.PreferredExecutionOrder)
            .Select(definition => definition.item.Rule).ToList();

         logger.LogDebug("Final sorted rules: {SortedRules}", string.Join(", ", this.rules.Select(rule => rule.GetType().Name)));
      }

      /// <summary>
      /// Verifies that all registered validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <typeparam name="TValidationRule">The type of the validation rules.</typeparam>
      /// <param name="rules">The rules to verify.</param>
      protected virtual List<RuleDefinition> VerifyValidationRules()
      {
         // ensures that registered rules dependencies are honored
         List<Type> GetRequiredRules(Type ruleType)
         {
            return ruleType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
               .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
               .ToList();
         }

         var definitions = new List<RuleDefinition>();

         Type validationRulesType = typeof(TValidationRule);

         using IDisposable logScope = logger.BeginScope("Verifying validation rules for {ValidationRuleType}", validationRulesType.Name);
         foreach (TValidationRule rule in this.rules)
         {
            RulePrecedenceAttribute? precedenceAttribute = rule.GetType().GetCustomAttribute<RulePrecedenceAttribute>();
            var definition = new RuleDefinition(rule, precedenceAttribute?.PreferredExecutionOrder ?? RulePrecedenceAttribute.DefaultExecutionOrder);
            definitions.Add(definition);

            if (precedenceAttribute != null)
            {
               foreach (Type? ruleToExecuteBefore in precedenceAttribute.MustBeExecutedAfter)
               {
                  TValidationRule? ruleInstance = rules.FirstOrDefault(rule => ruleToExecuteBefore.IsAssignableFrom(rule.GetType()));
               }
            }

            Type ruleType = rule!.GetType();
            foreach (Type requiredRule in GetRequiredRules(ruleType))
            {
               if (!validationRulesType.IsAssignableFrom(requiredRule))
               {
                  ThrowHelper.ThrowArgumentException($"{nameof(ruleType)} must implement {validationRulesType.Name}.");
               }

               TValidationRule? requiredRuleInstance = rules.FirstOrDefault(rule => requiredRule.IsAssignableFrom(rule.GetType()));
               if (requiredRuleInstance == null)
               {
                  ThrowHelper.ThrowArgumentException($"{nameof(ruleType)} requires '{requiredRule.Name}' but the rule (or a subclass of that rule) is not registered.");
               }

               definition.ExecuteAfter(requiredRuleInstance);
            }
         }

         return definitions;
      }
   }
}