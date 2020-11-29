using System.Net;

namespace MithrilShards.WebApi
{
   public class ApiServiceDefinition
   {
      public IPEndPoint? EndPoint { get; set; }

      /// <summary>
      /// Gets or sets the name of the endpoint, used to generate a SwaggerEndpoint.
      /// </summary>
      public string? Name { get; set; } = "Unnamed";

      public string SwaggerPath { get; set; } = "path";

      public string Version { get; set; } = "v1";

      public bool Https { get; set; } = false;
   }
}
