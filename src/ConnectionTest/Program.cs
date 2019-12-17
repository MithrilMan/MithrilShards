using System;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.P2P;

namespace ConnectionTest {
   class Program {
      static async Task Main(string[] args) {
         bool isServer = args.Contains("-server");

         try {
            if (isServer) {
               await StartServer(args);
            }
            else {
               await StartClient(args);
            }
         }
         catch (Exception ex) {
            Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
         }

      }

      private static async Task StartClient(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>()
            .Configure(args)
            .UseP2PForgeServer()
            .RunConsoleAsync();
      }

      private static async Task StartServer(string[] args) {
         await new ForgeBuilder()
           .UseForge<Forge>()
           .Configure(args)
           .UseP2PForgeServer()
           .RunConsoleAsync();
      }
   }
}
