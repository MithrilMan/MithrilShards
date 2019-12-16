using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Core.Forge {
   public class ForgeBuilder : IForgeBuilder {
      /// <summary>
      /// The forge is built
      /// </summary>
      public bool forgeIsBuilt;
      private IForge forge;

      public ForgeSettings HostSettings => throw new NotImplementedException();

      private IServiceCollection services;

      private ServiceProvider forgeServiceProvider;

      /// <summary>List of service configuration actions that has to be called in order to add custom services to DI container.</summary>
      private readonly List<Action<IServiceCollection>> registerServicesActions;

      public ForgeBuilder() {
         this.registerServicesActions = new List<Action<IServiceCollection>>();
      }

      public IForgeBuilder RegisterServices(Action<IServiceCollection> registerServices) {
         if (registerServices is null) {
            throw new ArgumentNullException(nameof(registerServices));
         }

         this.registerServicesActions.Add(registerServices);
         return this;
      }

      public IForge Build() {
         if (this.forgeIsBuilt) {
            throw new ForgeBuilderException("The forge has been already built.");
         }

         this.services = this.BuildServices();
         this.forgeServiceProvider = this.services.BuildServiceProvider();
         this.forge = this.forgeServiceProvider.GetService<IForge>();

         this.forgeIsBuilt = true;
         return this.forge;
      }

      public void Run() {
         if (!this.forgeIsBuilt) {
            throw new ForgeBuilderException("The forge has not been built yet, call Build() first.");
         }

         IForgeLifetime forgeLifetime = this.forgeServiceProvider.GetRequiredService<IForgeLifetime>();

         Console.CancelKeyPress += (sender, eventArgs) => {
            Console.WriteLine("Application is shutting down...");
            try {
               this.forge.ShutDown();
            }
            catch (ObjectDisposedException exception) {
               Console.WriteLine(exception.Message);
            }
            // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
            eventArgs.Cancel = true;
         };


         IEventBus eventbus = this.forgeServiceProvider.GetService<IEventBus>();

         using (SubscriptionToken sub = eventbus.Subscribe<Events.ForgeShuttedDown>(action => {
            Console.WriteLine();
            Console.WriteLine("Application stopped.");
            Console.WriteLine();
         })) {

            Console.WriteLine();
            Console.WriteLine("Application starting, press Ctrl+C to cancel.");
            Console.WriteLine();

            this.forge.Start();

            Console.WriteLine();
            Console.WriteLine("Application started, press Ctrl+C to stop.");
            Console.WriteLine();

            forgeLifetime.ForgeShuttingDown.WaitHandle.WaitOne();
         };
      }

      /// <summary>
      /// Starts a forge instance, sets up cancellation tokens for its shutdown, and waits until it terminates.
      /// </summary>
      /// <param name="cancellationToken">Cancellation token that triggers when the node should be shut down.</param>
      public void BuildAndRun() {
         this.Build();
         this.Run();
      }

      /// <summary>
      /// Constructs and configures services ands features to be used by the node.
      /// </summary>
      /// <returns>Collection of registered services.</returns>
      private IServiceCollection BuildServices() {
         this.services = new ServiceCollection();

         // register services before features
         // as some of the features may depend on independent services
         foreach (Action<IServiceCollection> configureServices in this.registerServicesActions) {
            configureServices(this.services);
         }

         return this.services;
      }
   }
}
