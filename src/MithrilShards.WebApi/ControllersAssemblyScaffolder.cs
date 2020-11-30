using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MithrilShards.WebApi
{
   /// <summary>
   /// Class used to discover Controllers in assembly that aren't exposed by a Shard and have controllers that
   /// have to be added to a WEB API service.
   /// </summary>
   public class ControllersAssemblyScaffolder
   {
      private readonly List<Assembly> _assembliesToScaffold = new List<Assembly>();
      internal ControllersAssemblyScaffolder()
      {
      }

      /// <summary>
      /// Loads the assembly where <typeparamref name="T"/> is defined.
      /// Use this to let the forge find controllers on assemblies that aren't exposed by an used <see cref="MithrilShards.Core.MithrilShards.IMithrilShard"/>.
      /// </summary>
      /// <typeparam name="T">The type that's defined in an assembly that contains controllers.</typeparam>
      public ControllersAssemblyScaffolder LoadAssemblyFromType<T>()
      {
         _assembliesToScaffold.Add(typeof(T).Assembly);
         return this;
      }

      internal IEnumerable<Assembly> GetAssemblies()
      {
         return _assembliesToScaffold.Distinct();
      }
   }
}
