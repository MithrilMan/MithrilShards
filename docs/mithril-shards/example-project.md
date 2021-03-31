---
title: Example Project
description: Mithril Shards implementation, Example Project
---

--8<-- "refs.txt"

The best way to see it in action is by inspecting the Example project I've created.

## Example Projects

The example is composed by several projects, each one with their own scope, to mimic a (simple) typical modular application:

* MithrilShards.Example
* MithrilShards.Example.Network.Bedrock
* MithrilShards.Example.Dev
* MithrilShards.Example.Node

### MithrilShards.Example

It contains the core classes and services needed to run the example application.
Here we can find:

* network classes like
  * a custom IPeerContext implementation and its factory class
  * some custom `IServerPeerConnectionGuard` implementation to filter incoming connections and a custom ConnectorBase implementation that contains the logic to connect to other peers
* protocol classes like
  * `INetworkMessage` implementations of custom messages (payloads) and complex types used within their implementation.
  * `INetworkMessage` and type serializators that serialize classes into a byte representation that can be sent through the network.
  * `INetworkMessage` processors that contain the logic to parse incoming messages and send messages to other peers
* plumbing classes like shard setting class and services used by processors or other internal components.



It makes use of [System.CommandLine](https://github.com/dotnet/command-line-api){:target="_blank"} to have implement the application as a CLI.

```
using System.CommandLine;
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

namespace MithrilShards.Example.Node
{
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
}
```