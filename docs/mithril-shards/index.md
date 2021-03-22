---
title: Overview
description: Mithril Shards implementation overview
---
--8<-- "refs.txt"

## Architecture Overview

Mithril Shards is a modular framework for building P2P Applications that can be expanded by additional features like WEB Api endpoints, MQ based services, SignalR hubs, cross platform UI and much more.

Pretending to be into a Tolkien universe, I thought of defining features as *shards*, where each shard of mithril can be put into a forge and fused togheter with other mithril shards, to create a final artifact.

To find analogies with .net naming conventions:

- **Forge** (to be more precise, **ForgeBuilder**) is a [HostBuilder](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder?view=dotnet-plat-ext-5.0){:target="_blank"} on steroids (actually it encapsulate a HostBuilder instance).
- **Shard** is an application part that gets merged into the hostbuilder, using dependency injection, extending its functionality 
- **Artifact** is just an allegoric view of the result of `forgeBuilderInstance.RunConsoleAsync()`.

!!! note
	Current naming may be subject to changes.

To build an application using these concepts, we have to create a ForgeBuilder, specify which Forge type to use and then we can put into the forge all the shards we need by calling `AddShard` method.

Since a shard may require complex configurations and inject service implementations it may need, usually you don't want to call that method directly but instead you'd want to have a `IForgeBuilder` extension that you can put in a class in your shard project, where your initialization logic happens.
That's how .Net core features and services are injected into the host builder and I think it's a good thing to use a similar approach because would be more friendly to devs used to .Net conventions.

After all shards are added and ForgeBuilder is started by `RunConsoleAsync`, the forge will take care of all the plumbing stuff, initializing all the shards.

The entry point is our forge builder class.
After declaring an instance we have to specify which implementation of Forge we want to use.
Actually the only available implementation is [DefaultForge]



## Example project

The best way to see it in action is by inspecting the Example project I've created for that purpose.
