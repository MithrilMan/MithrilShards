---
title: Example Projects Overview
description: Mithril Shards Example Projects, Example Projects Overview
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



## Example Projects

The example is composed by several projects, each one with their own scope, to mimic a (simple) typical modular application:

* [MithrilShards.Example]
* [MithrilShards.Example.Network.Bedrock]
* [MithrilShards.Example.Dev]
* [MithrilShards.Example.Node]

Each project has its own documentation page to present its purpose and to explain some implementation details, however all the code is well commented so you shouldn't have any problems understanding it, in any case the [Discussions]{:target="_blank"} on my repository is open for you.