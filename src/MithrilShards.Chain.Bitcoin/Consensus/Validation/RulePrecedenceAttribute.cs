using System;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Allow to define validation rule execution order.
   ///
   /// If a rule has to be executed before others, it can make use of <see cref="PreferredExecutionOrder"/> to specify when it should be executed.
   /// The lower that value is, the sooner the rule will be executed.
   /// Multiple rules sharing the same value will be executed in an undefined order if they don't make use of <see cref="RequiresRuleAttribute"/> or
   /// <see cref="MustBeExecutedAfter"/>.
   ///
   /// <see cref="MustBeExecutedAfter"/> can be used to specify that current rule has to be executed <b>AFTER</b> the specified rules.
   /// In case listed rules aren't part of current rule set, they are ignored.
   /// </summary>
   /// <seealso cref="System.Attribute" />
   [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
   sealed class RulePrecedenceAttribute : Attribute
   {
      /// <summary>
      /// The default execution order when no execution order is specified.
      /// </summary>
      public const uint DEFAULT_EXECUTION_ORDER = 100;

      /// <summary>
      /// Specify when the target rule should be executed.
      /// The lower that value is, the sooner the rule will be executed.
      /// Multiple rules sharing the same value will be executed in an undefined order if they don't make use of <see cref="RequiresRuleAttribute"/> or
      /// <see cref="MustBeExecutedAfter"/>.
      /// </summary>
      public uint PreferredExecutionOrder { get; }

      /// <summary>
      /// An optional list of other rules that have to be executed after the target rule.
      /// In case listed rules aren't part of current rule set, they are ignored.
      /// Use <see cref="RequiresRuleAttribute"/> to execute the target rule after a specific rule and be sure that the rule exists
      /// (e.g. when target rule needs some contextual date set by another rule).
      /// </summary>
      public Type[] MustBeExecutedAfter { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="RulePrecedenceAttribute" /> class.
      /// </summary>
      /// <param name="preferredExecutionOrder">The preferred execution order (the lower, the earlier we want to execute the rule).</param>
      /// <param name="mustBeExecutedAfter">An optional list of other rules that have to be executed after the target rule.</param>
      public RulePrecedenceAttribute(uint preferredExecutionOrder = DEFAULT_EXECUTION_ORDER, params Type[] mustBeExecutedAfter)
      {
         PreferredExecutionOrder = preferredExecutionOrder;
         MustBeExecutedAfter = mustBeExecutedAfter;
      }
   }
}
