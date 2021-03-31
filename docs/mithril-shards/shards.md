---
title: Understanding Shards
description: Mithril Shards implementation, Understanding Shards
---
--8<-- "refs.txt"

## What is a shard

When you think about a Mithril Shard, you have to think of it as a set of services that extend the capabilities of the application you are building.  
In layman's terms shards can augment the final application with additional functionality (features) and this is the core concept of a modular application framework.

When you build an application using Mithril Shards you are like a blacksmith in an epic fantasy novel: you chose which shards of mithril to use, put them into the forge, melt them together and finally shape the final: your powerful artifact!

![image-20210331150929890](../img/image-20210331150929890.png){.zoom}  
<sup>I'd like to know the artist to give him credits for the image above.</sup>



But we are not living in a Tolkien novel, so what we do is :

- [x] choose the shards we need (either by creating them, forking their code or referencing their nuget package)
- [x] add them to the [ForgeBuilder] using some extension method that the shard developer has created for us and that may contains some optional parameters
- [x] configure shards based on their available settings and our needs in the application configuration file (by default it's `forge-settings.json` but can be changed when the IForge implementation is chosen by calling ForgeBuilder `UseForge` method)
- [x] execute the forge builder to run the program.



The [example project] contains code to show how to achieve this.
