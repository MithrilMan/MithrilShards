---
title: Overview
description: Mithril Shards implementation overview
---
--8<-- "refs.txt"

## Architecture Overview

Mithril Shards is a modular framework for building P2P Applications that can be expanded by additional features like Web API endpoints, MQ based services, SignalR hubs, cross platform UI and much more.

Pretending to be into a Tolkien universe, I thought of defining features as *shards*, where each shard of mithril can be put into a forge and fused together with other mithril shards, to create a final artifact.

To find analogies with .net naming conventions:

- **Forge** (to be more precise, **ForgeBuilder**) is a [HostBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder?view=net-8.0){:target="_blank"} on steroids (actually it encapsulate a HostBuilder instance).
- **Shard** is an application part that gets merged into the hostbuilder, using dependency injection, extending its functionality 
- **Artifact** is just an allegoric view of the result of `forgeBuilderInstance.RunConsoleAsync()`.

!!! note
	Current naming may be subject to changes.

To build an application using these concepts, we have to create a ForgeBuilder, specify which Forge type to use and then we can put into the forge all the shards we need by calling `AddShard` method.

Since a shard may require complex configurations and inject service implementations it may need, usually you don't want to call that method directly but instead you'd want to have a `IForgeBuilder` extension that you can put in a class in your shard project, where your initialization logic happens.
That's how .Net core features and services are injected into the host builder and I think it's a good thing to use a similar approach because would be more friendly to devs used to .Net conventions.

Each shard may be configured by a strong typed setting class that supports eager validation (would throw if the setting files contains invalid data) and each shard is responsible to register services it needs that would cooperate with the `IForge` implementation to perform needed tasks, more on this in the specific documentation sections.

After all shards are added and ForgeBuilder is started by `RunConsoleAsync`, the forge will take care of all the plumbing stuff, initializing all the shards.

For a detailed description of the components composing the Mithril Shards framework, refers to specific documentation pages.

The entry point of a Mithril Shards application is the [ForgeBuilder] class.

