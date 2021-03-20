<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**

- [Premise](#premise)
- [Project Overview](#project-overview)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

---
title: Overview
description: Mithril Shards bitcoin implementation overview
---
## Premise

This folder contains technical documentation that helps to understand the choices behind the implementation and design of current project.

Personally I'm not a fan of bitcoin core code base: even if it improved since early days, it's confusing, it lacks of an homogeneous design and architecture and suffers from old bad choices and above all lacks of proper technical documentation, following the mantra "source code is the documentation".

To be clear, I don't blame who contributes on bitcoin core, I'm just stating objective facts and I hope to fix some of these issues with my project but I don't pretend to have documentation always in sync with latest changes because it's a huge effort and I agree at some extents that source code is the final judge and I encourage you to dig into it if you want to go deeper in implementation details or verify that an information written here is correct and please, if you find any issue about documentation, feel free to open an issue and I'll be happy to fix it (even a PR with your corrections can be valuable!).

To me a proper documentation doesn't have to explain the source code (source code should be as much readable as possible) but it should give insights about the process that leads toward specific implementations and have indications about good practice to work within the built library.


Beside this premise, I'm writing this documentation as if I were the target, in order to have a maintainable and easy to follow repository and of course be able to give good understanding about how to extend this project further with custom features (shards) to people that may be interested.

Nevertheless I would be happy to know that it could be useful to someone who may find it interesting and help in understanding why an approach has been chosen over another!

Also if you have question or want to discuss about technical details, you can use the repository [Discussion section](https://github.com/MithrilMan/MithrilShards/discussions).




## Project Overview

**Bitcoin Mithril Shard** has been built on top of **Mithril Shards** framework.
Mithril Shards goal is to be a framework and toolkit to build *<u>modular and distributed/P2P applications using .Net 5</u>* stack, focusing both on good design, good practices and performance.

Core functionalities can be glued togheter to compose the needed application, ranging from a P2P network layer, WEB Api layer, Diagnostic tools, cross platform UI based on blazor, distributed eventing using SignalR, MQ brokers like RabbitMq or any kind of other useful libraries.

Thanks to its design, anyone can build it's own Shard to create other features that can be used by Mithril Shards community.

More details about Mithril Shards can be found on the main documentation (TODO).

Bitcoin shard is a very good example about how to build a fully functional full node for bitcoin, leveraging all the juicy features that Mithril Shards exposes.

- Network layer is implemented by leveraging [Bedrock Framework](https://github.com/davidfowl/BedrockFramework) for the TCP implementation using both Client and Server connections.
  Data is serialized thanks to an well defined set of interfaces and classes that allows to implement an easy to read and maintain code.
- Incoming messages are dispatched to "Message Processors" that allow to handle the application logic following a good practice of separation of concerns.
- Meaningful events are dispatched using a *message bus* implementation that can reach any component in any application layer.
- A WEB Api infrastructure allow to create WEB Api endpoint easily and each feature can have its set of API published on different document specifications.
  Swagger is used as a UI to expose these API documents and allows to execute these APIs straight from that interface.
- For development/debugging purpose, a Shard inject some useful endpoint to inspect internal details of the running application.
- Logging is done using structured logging, makes use of Serilog to persist them and a configuration example shows how to use [Seq](https://datalust.co/seq) to have a very good UI to view logs, filter them, etc...
- Blazor is used to implement a cross platform UI as a companion for the full node.

