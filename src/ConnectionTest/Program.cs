using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Chain.Bitcoin;
using MithrilShards.Chain.Bitcoin.Network.Bedrock;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Diagnostic.StatisticsCollector;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Serilog;

namespace ConnectionTest
{
   static class Program
   {
      static async Task Main(string[] args)
      {

         await StartBedrockForgeServerAsync(args).ConfigureAwait(false);
      }

      private static async Task StartBedrockForgeServerAsync(string[] args)
      {
         await BuildForge(args)
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer<BitcoinNetworkProtocolMessageSerializer>()
            .UseStatisticsCollector()
            .UseDevController()
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
                  .UseBitcoinChain(networkName: network, minimumSupportedVersion: KnownVersion.V70012, currentVersion: KnownVersion.CurrentVersion);
            case "bitcoin-regtest":
               return new ForgeBuilder()
                  .UseForge<DefaultForge>(args, "forge-settings-bitcoin-regtest.json")
                  .UseBitcoinChain(networkName: network, minimumSupportedVersion: KnownVersion.V70012, currentVersion: KnownVersion.CurrentVersion);
            default:
               Console.WriteLine($"UNKNOWN NETWORK specified in -network argument: {network}. Fallback to Bitcoin-Main");
               throw new ArgumentException("Unknown Network");
         }
      }
   }
}
