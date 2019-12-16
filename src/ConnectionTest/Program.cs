using System;
using System.Linq;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Forge;

namespace ConnectionTest {
   class Program {
      static void Main(string[] args) {
         bool isServer = args.Contains("-server");

         try {
            if (isServer) {
               StartServer();
            }
            else {
               StartClient();
            }
         }
         catch (Exception ex) {
            Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
         }

      }

      private static void StartClient() {
         new ForgeBuilder()
            .UseForge()
            .BuildAndRun();
      }

      private static void StartServer() {
         new ForgeBuilder()
            .UseForge()
            .BuildAndRun();
      }
   }
}
