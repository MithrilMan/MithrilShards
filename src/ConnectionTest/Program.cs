using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Chain.Bitcoin;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Diagnostic.StatisticsCollector;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using MithrilShards.Network.Legacy;
using Serilog;

namespace ConnectionTest
{
   static class Program
   {
      static async Task Main(string[] args)
      {

         await StartBedrockForgeServerAsync(args).ConfigureAwait(false);
         //await StartP2PForgeServer(args).ConfigureAwait(false);
      }

      private static async Task StartBedrockForgeServerAsync(string[] args)
      {
         await BuildForge(args)
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer()
            .UseStatisticsCollector()
            .UseDevController()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }

      private static async Task StartP2PForgeServerAsync(string[] args)
      {
         await BuildForge(args)
            .UseSerilog("log-settings.json")
            .UseP2PForgeServer()
            .UseStatisticsCollector()
            .RunConsoleAsync()
            .ConfigureAwait(false);
      }

      private static IForgeBuilder BuildForge(string[] args)
      {
         string network = args
            .DefaultIfEmpty("--network=bitcoin-main")
            .Where(arg => arg.StartsWith("--network", ignoreCase: true, CultureInfo.InvariantCulture))
            .Select(arg => arg.Replace("--network=", string.Empty, ignoreCase: true, CultureInfo.InvariantCulture))
            .FirstOrDefault();

         Console.WriteLine($"Building {network} forge...");

         switch (network)
         {
            case "bitcoin-main":
               return new ForgeBuilder()
                  .UseForge<DefaultForge>(args)
                  .UseBitcoinChain<BitcoinMainDefinition>(minimumSupportedVersion: KnownVersion.V209, currentVersion: KnownVersion.CurrentVersion);
            case "bitcoin-regtest":
               return new ForgeBuilder()
                  .UseForge<DefaultForge>(args, "forge-settings-bitcoin-regtest.json")
                  .UseBitcoinChain<BitcoinRegtestDefinition>(minimumSupportedVersion: KnownVersion.V209, currentVersion: KnownVersion.CurrentVersion);
            default:
               Console.WriteLine($"UNKNOWN NETWORK specified in -network argument: {network}. Fallback to Bitcoin-Main");
               throw new ArgumentException("Unknown Network");
         }
      }
   }
}
