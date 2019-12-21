using System;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.P2P;
using MithrilShards.Logging.Serilog;
using Serilog;
using MithrilShards.Chain.Bitcoin;
using MithrilShards.P2P.Bedrock;

namespace ConnectionTest {
   class Program {
      static async Task Main(string[] args) {

         await StartBedrockForgeServer(args).ConfigureAwait(false);
         //await StartP2PForgeServer(args).ConfigureAwait(false);
      }

      private static async Task StartBedrockForgeServer(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings.json")
            .UseBedrockForgeServer()
            .UseBitcoinChain()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }

      private static async Task StartP2PForgeServer(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings.json")
            .UseP2PForgeServer()
            .UseBitcoinChain()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }
   }
}
