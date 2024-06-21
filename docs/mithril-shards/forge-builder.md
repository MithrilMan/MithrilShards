---
title: ForgeBuilder
description: Mithril Shards implementation, ForgeBuilder class
---

--8<-- "refs.txt"

ForgeBuilder class represents the entry point of a Mithril Shards application, it allows to add a shard by calling the generic `AddShard` method, with different overloads that accept an optional strongly typed shard setting file with an optional setting file validator.

By using `ConfigureLogging` it's possible to configure logging, it's basically a wrapper on  the inner [HostBuilder ConfigureLogging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.webhostbuilder.configurelogging?view=aspnetcore-1.1&viewFallbackFrom=aspnetcore-8.0){:target="_blank"} method, you could use it to have a finer control over logging configuration and available providers, but the easier way to log is by using the available [SerilogShard] that uses Serilog to configure the logging and relies on a configurable setting file where you can specify which sink to use.
You can find more details on its specific documentation page and an example of its usage in the [example project]

After declaring an instance we have to specify which implementation of Forge we want to use.
Actually the only available implementation is [DefaultForge] class.



