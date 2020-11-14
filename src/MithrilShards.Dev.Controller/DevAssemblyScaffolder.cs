using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MithrilShards.Dev.Controller
{
   public class DevAssemblyScaffolder
   {
      private readonly List<Assembly> assembliesToScaffold = new List<Assembly>();
      internal DevAssemblyScaffolder()
      {
      }

      public DevAssemblyScaffolder LoadAssemblyFromType<T>()
      {
         this.assembliesToScaffold.Add(typeof(T).Assembly);
         return this;
      }

      internal IEnumerable<Assembly> GetAssemblies()
      {
         return this.assembliesToScaffold.Distinct();
      }
   }
}
