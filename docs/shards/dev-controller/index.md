---
title: Overview
description: Mithril Shards, DevControllerShard
---
--8<-- "refs.txt"

DevControllerShard is a shard that depends on [WebApiShard], it's goal is to inject some useful controllers meant to be used in DEV area.

To add DevControllerShard into the forge you can use the `IForgeBuilder` extension `UseDevController`.

!!! note
	This shard register a new [ApiServiceDefinition] for the dev area, so if you plan implementing a custom feature that aims to add one or more controller to DEV area, remember to add this shard or register yourself the DEV area.

Current available controllers are [PeerManagementController](#PeerManagementController), [ShardsController](#ShardsController) and [StaticsController](#StaticsController).

## PeerManagementController

This controller exposes actions useful to connect or disconnect from a specific peer.  
It leverages `RequiredConnection` service whose task is to periodically try to connect to a issued list of peers.

The list of peers can be issued both by Connect API or by configuration file using the `Connect` property of `ForgeConnectivitySettings` settings, used by [BedrockForgeConnectivityShard].

