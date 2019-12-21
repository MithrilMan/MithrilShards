using System;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.P2P;
using MithrilShards.Logging.Serilog;
using Serilog;

namespace ConnectionTest {
   class Program {
      static async Task Main(string[] args) {
         bool isServer = args.Contains("-server");

         try {
            if (isServer) {
               await StartServer(args).ConfigureAwait(false);
            }
            else {
               await StartClient(args).ConfigureAwait(false);
            }
         }
         catch (Exception ex) {
            Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.Message);
         }

      }

      private static async Task StartClient(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings.json")
            .UseP2PForgeServer()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }

      private static async Task StartServer(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings.json")
            .UseP2PForgeServer()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }
   }
}
