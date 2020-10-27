using System;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Allow to define validation rule dependencies.
   /// If a rule define this attribute and the specified rule type isn't registered, the Forge
   /// will throw during startup complaining about missing rule.
   /// </summary>
   /// <seealso cref="System.Attribute" />
   [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
   sealed class RequiresRuleAttribute : Attribute
   {
      /// <summary>
      /// Gets the required rule type.
      /// </summary>
      public Type RequiredRuleType { get; }

      public RequiresRuleAttribute(Type requiredRuleType)
      {
         this.RequiredRuleType = requiredRuleType;
      }
   }
}
