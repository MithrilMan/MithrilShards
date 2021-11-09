---
title: Event Bus
description: Mithril Shards implementation, Event Bus

---

--8<-- "refs.txt"

Event Bus is a simple implementation of the [publish/subscribe pattern](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern){:target="_blank"} which is a messaging pattern that allows two or more actors to publish messages and handle these messages without having a direct relationship between them.

Publishers don't know if the message they are publishing is handled and by whom, while subscribers do not know who was the publisher of a specific event: this means that components among Mithril Shards can be [loosely coupled](https://en.wikipedia.org/wiki/Loose_coupling){:target="_blank"} 

