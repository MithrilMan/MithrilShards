---
title: Welcome!
description: Mithril Shards home page
---
--8<-- "refs.txt"

## Goal

Mithril Shards goal is to implement a .NET 6 extensible P2P network & distributed services library from scratch with focus on architecture and performance.

Allows you to define custom network serialization protocol, easily handle payload messages and interact with the software leveraging any available features (named Shards) like Web API endpoints, cross platform Blazor UI,  and a lot of other exciting stuffs that community can implement and release to the public too!

The project is very ambitious and it's currently developed just by me as a pet project but a huge effort has already been made and some part of this unique code base has been reused in other blockchain technologies to improve their performance.



## Current Tech

A random list of available tech used within Mithril Shards.

- [.Net 6](https://dotnet.microsoft.com/download/dotnet/6.0){:target="_blank"} - ... for everything.
- [Bedrock Framework](https://github.com/davidfowl/BedrockFramework/){:target="_blank"} - TCP/IP default connectivity implementation.
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore){:target="_blank"} - to handle Web API in a configurable, multi-area environment and have a playground to test APIs with swagger.
- [Serilog](https://github.com/serilog/serilog-aspnetcore){:target="_blank"} - default logging implementation.
- [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet){:target="_blank"} - a benchmark framework, very handy to benchmark different approach during implementation phases.



## How things started

I have DLT experience in the past years and one of my previous experience has been working for a blockchain tech firm that had a FN implementation in C#.
I really love C# and .Net core has improved a lot performance too.

Working on their codebase I saw lot of things that could have been improved, both in design and in performance.
Their implementation started as a kind of 1-1 porting of bitcoin core source with all the cons that it brings.

So I thought about diving into this titanic effort of creating a full node in C# starting from scratch, mainly to go as deeper as possible into technical details, trying to focus both on a proper architecture design and performance improvements.

One of the first thing I implemented was of course the basic handshake process between two nodes and to achieve that, I started by following bitcoin core source because unluckily that's the only part that contains kind of specifications: it's hard to find a detailed updated technical documentations, even this [protocol documentation](https://en.bitcoin.it/wiki/Protocol_documentation){:target="_blank"} page contains wrong information, even if it's still useful.

I started using .Net TCP classes, using an internal state machine to handle peer connection statuses, then I found [Bedrock Framework](https://github.com/davidfowl/BedrockFramework){:target="_blank"}, that allowed me to abstract better my code and rely on it for the low level connection stuff (at the time of writing this documentation, that library is still in alpha and my concern about that project activity [has been appeased](https://github.com/davidfowl/BedrockFramework/issues/105){:target="_blank"} ).

As soon as I started defining properly my design, I found it interesting to abstract most of the stuff into an agnostic library that would allows me to create a P2P application in a modular way and attach additional features when needed, and this is how Mithril Shards started.

Since then I added more and more stuff, both for generic Mithril Shards project and for specific Bitcoin needs.

The multi project [Example Shard](https://github.com/MithrilMan/MithrilShards/tree/master/src/MithrilShards.Example){:target="_blank"} showcases how you can create a custom P2P software leveraging networking, custom messages, Web API endpoints Diagnostic tools, everything with a proper logging system.



## Why the Mithril Shards name?

Well... let's bullet some facts

- I like fantasy a lot, [J.R.R. Tolkien](https://en.wikipedia.org/wiki/J._R._R._Tolkien){:target="_blank"} of course has been one of my reads and mithril is a fictional metal in his universe.
- The main properties of [mithri](https://en.wikipedia.org/wiki/Mithril#Properties){:target="_blank"} are: being very strong, light and in its pure form very malleable to work with.
- I'm a developer who likes to engineer my software to be extensible and solid.
- My github handle is MithrilMan, guess what?

Now take these information, mix them up, and you'll see that to I aim to have a robust, fast and flexible project!
So this explain mithril, while about *Shards*, is because I see this project as a mix of shards that can be assembled together to give you a precious artifact!

In fact you shouldn't be surprised that the root class is called Forge... who says we can't have a bit of fun while designing a software?



## Call to Action!

!!! tip "Join to give feedback, ask for features, support, etc."
	Discord server: [https://discord.gg/T9kyKz4bAu](https://discord.gg/T9kyKz4bAu){:target="_blank"}  



## CI

| Current status                                               |
| ------------------------------------------------------------ |
| [![Main Build](https://github.com/MithrilMan/MithrilShards/actions/workflows/main-build.yml/badge.svg)](https://github.com/MithrilMan/MithrilShards/actions/workflows/main-build.yml){:target="_blank"} |