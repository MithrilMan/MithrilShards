---
title: Example Project Overview
description: Mithril Shards Example Projects, Example Project Overview
---

--8<-- "refs.txt"

The best way to see it in action is by inspecting the Example projects I've created.  
It's a multi-project example where each project plays its role into the modular application architecture.

Its goal is to show how to make use of Mithril Shards to implement a P2P application that implements a custom Web API controller, a custom network implementation and its own protocol with custom messages and serializators.

It reuses some other standard shards like :

- [x] [BedrockNetworkShard]
- [x] [StatisticCollectorShard]
- [x] [SerilogShard]
- [x] [WebApiShard]
- [x] [DevControllerShard]



What it does is quite simple:

You can run two instance of this project to connect to each other and every 10 seconds a ping message will be sent to the other peer, with a random quote message.

The quote message is randomly picked by the QuoteService and quotes can be manipulated by using the ExampleController exposed by the Web API.

All the code is well commented so you shouldn't have problem understanding it, in any case the [Discussions]{:target="_blank"} on my repository is open for you.



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



### MithrilShards.Example.Network.Bedrock

Contains few classes that are implementing the `INetworkProtocolMessageSerializer` interface needed by the [BedrockNetworkShard] shard to perform message serialization.
In this example we are mimicking bitcoin protocol that uses a magic word (4 bytes) that mark the start of a new message and it's message layout to define the rule to decode and encode messages over the network (see ProtocolDefinition.cs file).

Note how the code is really small and how it's easy to define custom network serialization of messages.
Current implementation relies on Bedrock framework shard, but if you want to create another lol level network implementation you are free to do so, you don't have to change anything else except this project to make use of your new low level network protocol, everything is abstracted out in the Mithril Shards Core project!!



### MithrilShards.Example.Dev

Contains just a Controller that expose a couple of Web API actions to manipulate the QuoteService and list, add and remove quotes.

In order to show an alternative way to register controllers, this project doesn't implement a shard and doesn't have any `Add*` / `Use*` extension method to add its share, instead its controller is discovered using the `ControllersSeeker` property of [WebApiShard] in its UseApi extension method..



### MithrilShards.Example.Node

It makes use of [System.CommandLine](https://github.com/dotnet/command-line-api){:target="_blank"} to have implement the application as a CLI.

While all other projects were C# Class Library projects, this one produces an executable that's the actual, assembled application.

It contains the Program.cs file that melt the shards into the forge and run it, plus a couple of configuration files that you can inspect to see different configuration combinations.

Program.cs file is quote short and easy to read:

```c#
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



## Running the example

Running just an instance doesn't fully show you how the program behave, it needs at least 2 peers to connect to each other, that's why there are already multiple configuration files configured differently to let you connect two instances together.

You can run one instance by setting `MithrilShards.Example.Node` as the startup project and run the launchSettings profile "node1" .

Then open a shell at the MithrilShards.Example.Node project path and run the command below

```sh
dotnet run --no-build --settings forge-settings2.json
```

This would cause you to have a debuggable instance running with the configuration defined in `forge-settings.json` file and another run running on the `forge-settings2.json` configuration.

Alternatively you can run both instances, without a debugger (but you can attach later the process to Visual Studio) by running on two different shells:

```sh
dotnet run --no-build --settings forge-settings.json
dotnet run --no-build --settings forge-settings2.json
```

The program running forge-settings.json contains the most verbose log level and you'll have the best experience if you install (or use a docker image) of [Seq](https://datalust.co/seq){:target="_blank"} configured on the port specified in your configuration file (e.g. localhost:5341).  
See [SerilogShard] for more details.

Here an example of the configuration file (forge-settings.json)

```json
{
   "ForgeConnectivity": {
      "ForceShutdownAfter": 300,
      "MaxInboundConnections": 25,
      "AllowLoopbackConnection": false,
      "Listeners": [
         {
            "IsWhitelistingEndpoint": true,
            "Endpoint": "0.0.0.0:45051"
         },
         {
            "IsWhitelistingEndpoint": true,
            "Endpoint": "127.0.0.1:45052",
            "PublicEndpoint": "98.0.0.1:45011"
         }
      ]
   },
   "Example": {
      "MaxTimeAdjustment": 4200,
      "Connections": [
         {
            "Endpoint": "127.0.0.1:45061",
            "AdditionalInformation": "I'm cool!"
         }
      ]
   },
   "StatisticsCollector": {
      "ContinuousConsoleDisplay": false,
      "ContinuousConsoleDisplayRate": 5
   },
   "DevController": {
      "Enabled": true
   },
   "WebApi": {
      "EndPoint": "127.0.0.1:45020",
      "Enabled": true,
      "Https": false
   },

   "Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
      "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
      "WriteTo": [
         {
            "Name": "Console",
            "Args": {
               "IncludeScopes": true,
               "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
            }
         },
         {
            "Name": "Seq",
            "Args": { "serverUrl": "http://localhost:5341" }
         }
      ],
      "MinimumLevel": {
         "Default": "Debug",
         "Override": {
            "Bedrock.Framework": "Warning",
            "Microsoft": "Warning",
            "System": "Warning"
         }
      }
   }
}
```



### Some Screenshots

Here a screenshot that shows the content of the shell when running the node with settings = `forge-settings.json`

![image-20210331175542234](../img/image-20210331175542234.png){.zoom}

You can access the Swagger UI by opening the address https://127.0.0.1:45020/docs/index.html  
![image-20210331180535114](../img/image-20210331180535114.png){.zoom}

Here you can manipulate quotes using the Web API, or even manually attempt to connect to other peers using PeerManagement Connect action in the DEV area.

If you installed Seq, you can access the logs in a better way like shown here:  
![image-20210331180409700](../img/image-20210331180409700.png){.zoom}