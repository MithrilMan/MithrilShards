## Implement peer permissions.

On Bitcoin core, peer permissions are a way to specify special permissions to peers that are whitelisted or whitebinded.

- **Whitebind** allows to define an endpoint to which peers that connects to inherit the permissions defined for that whitebind.

  `[host]:port`

- **Whitelist** allows to define specific permissions to peers connecting from specific IP address or CIDR notated network
  `1.2.3.4`
  `1.2.3.0/24`

Bitcoin core syntax for such configuration is 

```
-whitelist=bloomfilter@127.0.0.1/32.
-whitebind=bloomfilter,relay,noban@127.0.0.1:10020
```

I've already implemented a class `BitcoinPeerPermissions` but I've not yet decided how to allow to specify such permissions.
Since my implementation is json friendly, I'd prefer to specify settings by config file in a more structured way (command line is still possible even if more awkward)





## Redesign classes to hold Chain state

Current `IChainState` implementation (and the interface itself) is a result of many iteration starting from different point and converging into something I'm not happy with.

These classes are very important so need a proper design and performance tuning.

Some important aspects are:

- the memory footprint required to host the current chain representation in memory
- the lookup speed during IBD and during data fetch to serve other peers
- the tradeoff between in-memory stored data and data on disk that has to be fetched per request (consider a caching layer)