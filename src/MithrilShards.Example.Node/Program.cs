﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Diagnostic.StatisticsCollector;
using MithrilShards.Example.Dev;
using MithrilShards.Example.Network.Bedrock;
using MithrilShards.Example.Protocol;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Serilog;

namespace MithrilShards.Example.Node;

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
               description: "Specify the path to the forge settings file.")
         };

      rootCommand.Description = "Example App";
      rootCommand.TreatUnmatchedTokensAsErrors = false;

      // Note that the parameters of the handler method are matched according to the names of the options
      rootCommand.Handler = CommandHandler.Create<string, string, int>(async (settings, logSettings, protocolVersion) =>
      {
         await new ForgeBuilder()
           .UseForge<DefaultForge>(args, settings)
           .UseSerilog(logSettings)
           .UseBedrockNetwork<ExampleNetworkProtocolMessageSerializer>()
           .UseStatisticsCollector(options => options.DumpOnConsoleOnKeyPress = true)
              /// we are injecting ExampleDev type to allow <see cref="MithrilShards.WebApi.WebApiShard"/> to find all the controllers
              /// defined there because only controllers defined in an included shard assemblies are discovered automatically.
              /// Passing ExampleDev will cause dotnet runtime to load the assembly where ExampleDev Type is defined and every
              /// controllers defined there will be found later during <see cref="MithrilShards.WebApi.WebApiShard"/> initialization.
              .UseApi(options => options.ControllersSeeker = (seeker) => seeker.LoadAssemblyFromType<ExampleDev>())
           .UseDevController()
           .UseExample(KnownVersion.V1, protocolVersion)
           .RunConsoleAsync()
           .ConfigureAwait(false);
      });

      await rootCommand.InvokeAsync(args).ConfigureAwait(false);
   }
}
