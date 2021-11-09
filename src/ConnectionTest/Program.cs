using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MithrilShards.Chain.Bitcoin;
using MithrilShards.Chain.Bitcoin.Dev;
using MithrilShards.Chain.Bitcoin.Network.Bedrock;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Diagnostic.StatisticsCollector;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Serilog;

namespace ConnectionTest;

static class Program
{
   static async Task Main(string[] args)
   {
      // Create a root command with some options
      var rootCommand = new RootCommand {
            new Option<string>(
               "--settings",
               getDefaultValue: () => "forge-settings.json",
               description: "Specify the path to the forge settings file."),
            new Option<string?>(
               "--log-settings",
               getDefaultValue: () => null,
               description: "Specify the path to the forge log settings file. If not specified, try to get logging information from the main forge settings file."),
            new Option<int>(
               "--protocol-version",
               getDefaultValue: () => KnownVersion.CurrentVersion,
               description: "Specify the protocol version to use.")
               .FromAmong(typeof(KnownVersion)
                          .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                          .Where(field=> field.IsLiteral && !field.IsInitOnly)
                          .Select(field => field.GetValue(null)?.ToString())
                          .Where(field => field!=null)
                          .ToArray()!
                          ),
            new Option<string>(
               "--network",
               getDefaultValue: () => "bitcoin-main",
               description: "Specify the network to connect to.")
               .FromAmong("bitcoin-main","bitcoin-regtest")
         };

      rootCommand.Description = "Mithril Shards - Bitcoin Test";
      rootCommand.TreatUnmatchedTokensAsErrors = false;

      // Note that the parameters of the handler method are matched according to the names of the options
      rootCommand.Handler = CommandHandler.Create<string, string, int, string>(async (settings, logSettings, protocolVersion, network) =>
      {
         Console.WriteLine($"Building {network} forge...");

         await new ForgeBuilder().UseForge<DefaultForge>(args, settings)
                                 .UseBitcoinChain(networkName: network, minimumSupportedVersion: Math.Min(KnownVersion.V70012, protocolVersion), currentVersion: protocolVersion)
                                 .UseBitcoinDev()
                                 .UseSerilog(logSettings)
                                 .UseBedrockNetwork<BitcoinNetworkProtocolMessageSerializer>()
                                 .UseStatisticsCollector(options => options.DumpOnConsoleOnKeyPress = true)
                                 .UseApi()
                                 .UseDevController()
                                 .RunConsoleAsync()
                                 .ConfigureAwait(false);
      });

      await rootCommand.InvokeAsync(args).ConfigureAwait(false);
   }
}
