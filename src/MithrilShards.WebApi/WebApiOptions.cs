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
      public Action<ControllersAssemblySeeker>? ControllersSeeker { get; set; }

      public ControllersAssemblySeeker Seeker { get; } = new ControllersAssemblySeeker();

      public bool EnablePublicApi { get; set; } = true;

      internal void DiscoverControllers()
      {
         ControllersSeeker?.Invoke(Seeker);
      }
   }
}
