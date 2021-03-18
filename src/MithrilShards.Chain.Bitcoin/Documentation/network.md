# Network



#### Table of Contents

[Overview](#overview)

[Peer Context](#peercontext)

___



# <a name="overview">Network Protocol</a>

The Bitcoin network protocol is a TCP protocol whos serializes messages starting with a special, 4 bytes, constant data that's called *Magic bytes*, followed by 12 bytes representing the *command name*, 4 bytes representing the *payload size* and 4 bytes with the checksum of the payload.
More specific information about bitcoin protocol [can be found here.](https://developer.bitcoin.org/reference/p2p_networking.html) 

Mithril Shards implements a low level stack of interfaces and implementations that allow to focus on the application logic instead on low level details.
A default implementation uses Bedrock Framework to leverage the low level communication between peers and this is what's used in the Bitcoin shard to provide P2P connectivity.

A typical Bitcoin full node (henceforth called *FN*) is able to connect to other nodes and accept incoming connections.
Before two peers can exchange information, they have to perform an handshake to prove that they can understand each other (more details on the the bitcoin network protocol can be found on this [bitcoin developer resource](https://developer.bitcoin.org/devguide/p2p_network.html)).

Before being able to handshake, whenever a connection has been established between two peers the FN stores some metadata about the remote peer. This data is stored into a class that implements IPeerContext interface and its implementation represents our next section.





## <a name="peercontext">Peer Context</a>

Default Mithril Shards implementation uses `PeerContext` class to store, amongh other thigns, informations like peer unique identification, direction (inbound/outbound) remote and local endpoints, user agent identification, negotiated protocol version and other attachable properties leveraging the .Net [IFeatureCollection](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.features.ifeaturecollection?view=aspnetcore-5.0) interface.

Bitcoin needs some additional information and some of the properties that are ubiquitous needed among all optional Bitcoin features (shards) like wallet, apis, indexer, etc... have been defined directly into `BitcoinPeerContext` that extends the defualt `PeerContext`. Some of the additional properties are Permissions (that may change the FN behavior based on its set) and TimeOffset, that's an important aspect for the consensus logic.

The peer context creation is handled by the core Mithril Shard network implementation and since it can't know about the `BitcoinPeerContext` properties, it rely on a peer context factory, in this case we are talking about `BitcoinPeerContextFactory` class.

It leverages the generic class `PeerContextFactory<>` and its implementation is bare bone, no need to override anything.

```c#
public class PeerContextFactory<TPeerContext> : IPeerContextFactory where TPeerContext : IPeerContext
```

Once the peer context has been context has been created, a sanity check is performed to see if the two peers can connect to each other before trying to handshake. The behavior is very similar to both inbound and outbound connections.

Network protocol is implemented through the serialization of classes which implement `INetworkMessage` interface and are decorated with `NetworkMessageAttribute` that works in synergy with an implementation of `INetworkMessageSerializer` to implement network serialization.

