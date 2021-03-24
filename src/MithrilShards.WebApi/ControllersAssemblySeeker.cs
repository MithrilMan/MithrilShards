using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MithrilShards.WebApi
{
   /// <summary>
   /// Class used to discover Controllers in assembly that aren't exposed by a Shard and have controllers that
   /// have to be added to a WEB API service.
   /// </summary>
   public class ControllersAssemblySeeker
   {
      private readonly List<Assembly> _assembliesToInspect = new List<Assembly>();
      internal ControllersAssemblySeeker()
      {
      }

      /// <summary>
      /// Loads the assembly where <typeparamref name="T"/> is defined.
      /// Use this to let the forge find controllers on assemblies that aren't exposed by an used <see cref="MithrilShards.Core.Shards.IMithrilShard"/>.
      /// </summary>
      /// <typeparam name="T">The type that's defined in an assembly that contains controllers.</typeparam>
      public ControllersAssemblySeeker LoadAssemblyFromType<T>()
      {
         _assembliesToInspect.Add(typeof(T).Assembly);
         return this;
      }

      internal IEnumerable<Assembly> GetAssemblies()
      {
         return _assembliesToInspect.Distinct();
      }
   }
}
