using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.WebApi
{
   public class WebApiOptions
   {

      /// <summary>
      /// Action that allow to discover controllers in assemblies that don't have an entry explicit point.
      /// </summary>
      public Action<ControllersAssemblyScaffolder>? AssemblyScaffoldEnabler { get; set; }

      public ControllersAssemblyScaffolder Scaffolder { get; } = new ControllersAssemblyScaffolder();

      public bool EnablePublicApi { get; set; } = true;

      internal void Scaffold()
      {
         AssemblyScaffoldEnabler?.Invoke(Scaffolder);
      }
   }
}
