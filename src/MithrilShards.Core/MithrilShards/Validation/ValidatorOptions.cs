using System;
using System.Collections.Generic;

namespace MithrilShards.Core.MithrilShards.Validation
{
   internal class ValidatorOptions
   {
      // Maps each options type to a method that forces its evaluation, e.g. IOptionsMonitor<TOptions>.Get(name)
      public IDictionary<Type, Action> Validators { get; } = new Dictionary<Type, Action>();
   }
}