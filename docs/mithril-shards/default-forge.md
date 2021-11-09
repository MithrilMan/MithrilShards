---
title: DefaultForge
description: Mithril Shards implementation, DefaultForge
---

--8<-- "refs.txt"

The entry point is our forge builder class.
After declaring an instance we have to specify which implementation of Forge we want to use.
Actually the only available implementation is DefaultForge.

DefaultForge is a simple class, it's implemented as a [BackgroundService](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice){:target="_blank"}, when the forge is built and ran by the ForgeBuilder, it automatically starts and its `ExecuteAsync` method is invoked.

In this method a default configuration file is generated in case it doesn't exists yet and all registered [shards] are started (their `InitializeAsync` method is invoked).

After all shards are initialized, they are started by invoking their `StartAsync` (non awaited) method.

From this moment, the forge is running.

When the application lifetime ends (in the default scenario by pressing CTRL+C when running in console) `StopAsync` method is called, which calls `StopAsync` on all running shards allowing them to close properly.