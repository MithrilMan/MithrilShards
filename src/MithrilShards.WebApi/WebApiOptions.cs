using System;

namespace MithrilShards.WebApi
{
   public class WebApiOptions
   {

      /// <summary>
      /// Action that allow to discover controllers in assemblies that don't have an entry explicit point.
      /// </summary>
      public Action<ControllersAssemblySeeker>? ControllersSeeker { get; set; }

      public ControllersAssemblySeeker Seeker { get; } = new ControllersAssemblySeeker();

      /// <summary>
      /// Gets or sets a value indicating whether public API are enabled.
      /// When creating an application where a public area is never needed, you may want to use this
      /// property rather than relying on external configuration file that may be missing or edited.
      /// </summary>
      public bool EnablePublicApi { get; set; } = true;

      /// <summary>
      /// The public API description
      /// </summary>
      public string PublicApiDescription { get; set; } = "Mithril Shards public API";

      /// <summary>
      /// Configures the Swagger UI title.
      /// </summary>
      public string Title { get; set; } = "Mithril Shards Web API";

      internal void DiscoverControllers()
      {
         ControllersSeeker?.Invoke(Seeker);
      }
   }
}
