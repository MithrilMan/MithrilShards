using System;

namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules
{
   /// <summary>
   /// Allow to define validation rule dependencies.
   /// If a rule define this attribute and the specified rule type isn't registered, the Forge
   /// will throw during startup complaining about missing rule.
   /// </summary>
   /// <seealso cref="System.Attribute" />
   [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
   sealed class ValidationRuleDependencyAttribute : Attribute
   {
      /// <summary>
      /// Gets the rule dependencies.
      /// </summary>
      public Type[] RuleDependencies { get; }

      public ValidationRuleDependencyAttribute(Type[] ruleDependencies)
      {
         this.RuleDependencies = ruleDependencies;
      }
   }
}
