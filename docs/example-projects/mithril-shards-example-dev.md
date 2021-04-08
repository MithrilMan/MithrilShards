---
title: MithrilShards.Example.Dev
description: Mithril Shards Example Projects, MithrilShards.Example.Dev
---

--8<-- "refs.txt"

### MithrilShards.Example.Dev

Contains just a Controller that expose a couple of Web API actions to manipulate the QuoteService and list, add and remove quotes.

In order to show an alternative way to register controllers, this project doesn't implement a shard and doesn't have any `Add*` / `Use*` extension method to add its share, instead its controller is discovered using the `ControllersSeeker` property of [WebApiShard] in its UseApi extension method..
