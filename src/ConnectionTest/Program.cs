using System;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Network;
using MithrilShards.Logging.Serilog;
using Serilog;
using MithrilShards.Chain.Bitcoin;
using MithrilShards.Network.Bedrock;
using MithrilShards.Chain.Bitcoin.Protocol;

namespace ConnectionTest {
   class Program {
      static async Task Main(string[] args) {

         await StartBedrockForgeServer(args).ConfigureAwait(false);
         //await StartP2PForgeServer(args).ConfigureAwait(false);
      }

      private static async Task StartBedrockForgeServer(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer()
            .UseBitcoinChain(minimumSupportedVersion: KnownVersion.V209, currentVersion: KnownVersion.CurrentVersion)
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }

      private static async Task StartP2PForgeServer(string[] args) {
         await new ForgeBuilder()
            .UseForge<Forge>(args)
            .UseSerilog("log-settings.json")
            .UseP2PForgeServer()
            .UseBitcoinChain(minimumSupportedVersion: KnownVersion.V209, currentVersion: KnownVersion.CurrentVersion)
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }
   }
}
