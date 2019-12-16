using System;
using Microsoft.Extensions.DependencyInjection;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Host builder allows constructing a host using specific components.
   /// </summary>
   public interface IForgeBuilder {
      /// <summary>User defined host settings.</summary>
      ForgeSettings HostSettings { get; }

      /// <summary>
      /// Adds services to the forge builder.
      /// </summary>
      /// <param name="registerServices">A method that allow to register and configure custom services to the builder.</param>
      /// <returns>The same <see cref="IForgeBuilder"/> instance to allow fluent code.</returns>
      IForgeBuilder RegisterServices(Action<IServiceCollection> registerServices);

      /// <summary>
      /// Constructs the <see cref="Forge"/> with the required features, services, and settings.
      /// </summary>
      /// <returns>Initialized <see cref="Forge"/>.</returns>
      IForge Build();

      void Run();

      void BuildAndRun();
   }
}
