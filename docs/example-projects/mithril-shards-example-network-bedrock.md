---
title: MithrilShards.Example.Network.Bedrock
description: Mithril Shards Example Projects, MithrilShards.Example.Network.Bedrock
---

--8<-- "refs.txt"

### MithrilShards.Example.Network.Bedrock

Contains few classes that are implementing the `INetworkProtocolMessageSerializer` interface needed by the [BedrockNetworkShard] shard to perform message serialization.
In this example we are mimicking bitcoin protocol that uses a magic word (4 bytes) that mark the start of a new message and it's message layout to define the rule to decode and encode messages over the network (see ProtocolDefinition.cs file).

Note how the code is really small and how it's easy to define custom network serialization of messages.
Current implementation relies on Bedrock framework shard, but if you want to create another lol level network implementation you are free to do so, you don't have to change anything else except this project to make use of your new low level network protocol, everything is abstracted out in the Mithril Shards Core project!!