---
title: Using WebApiShard
description: Mithril Shards, Using WebApiShard
---

--8<-- "refs.txt"

## Add WebApiService to the forge

To add the shard to the forge, the `IForgeBuilder` extension `UseApi` has to be called, passing optional `options` to further configure the service

```c#
public static IForgeBuilder UseApi(this IForgeBuilder forgeBuilder, Action<WebApiOptions>? options = null)
```



## WebApiOptions

WebApiOptions class allows to customize the discovery process that's responsible to find and register Web API controllers end include them into a specific [ApiServiceDefinition](#apiservicedefinition).

The discovery process happens during WebApiShard initialization: it generates an [ApplicationPart](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.applicationparts.applicationpart?view=aspnetcore-5.0){:target="blank"} for each registered shard that will include all discovered `MithrilControllerBase`(ControllerBase) types defined in the shard assembly.

### ControllersSeeker

Sometimes you may have a project that just holds Controllers but isn't exposed as a shard, in this scenario you can use WebApiOptions during the call of UseApi, to explicitly add an assembly to inspect for controllers.

The [example project] makes use of this when it builds the forge:

```c#
/// we are injecting ExampleDev type to allow <see cref="WebApi.WebApiShard"/> to find all the controllers
/// defined there because only controllers defined in an included shard assemblies are discovered automatically.
/// Passing ExampleDev will cause dotnet runtime to load the assembly where ExampleDev Type is defined and every
/// controllers defined there will be found later during <see cref="WebApi.WebApiShard"/> initialization.
.UseApi(options => options.ControllersSeeker = (seeker) => seeker.LoadAssemblyFromType<ExampleDev>())
```

By doing this WebApiOptions will create an Application part for each explicitly added assembly and all Controller types defined in that assembly will be found and added to the available controllers in their specific area.

### EnablePublicApi

This settings enables or disables the *public API area*.  
The ***public area*** corresponds to the [ApiServiceDefinition](#apiservicedefinition) that's responsible to enable all controllers assigned to the area `WebApiArea.AREA_API`. 

If you are creating an application where a public area is never needed, you may want to use this property rather than relying on external configuration file that may be missing or edited.

### PublicApiDescription

This settings allow to customize the description used to describe public API.  
Defaults to `Mithril Shards public API`.

### Title

Configures the Swagger UI page title, useful for branding.  
Defaults to `Mithril Shards Web API`.



## WebApiSettings

WebApiSettings is the class that holds configuration settings required by the shard to works.  
It contains few properties to configure the endpoint used to listen to API requests and its behavior:

| Property | Type   | Description                                                  | Default           |
| -------- | ------ | ------------------------------------------------------------ | ----------------- |
| EndPoint | string | IP address and port number on which the shard will serve its Web API endpoint, in the form of `ip_address:port`. | "127.0.0.1:45020" |
| Https    | bool   | Whether WEB API should be exposed on HTTPS.                  | false             |
| Enabled  | bool   | Whether WebApiShard is enabled or not. Disabling it would cause any shard depending on WebApiShard, such as custom controllers or custom areas, to be unable to be served. | true              |



Configuration properties can be set in the application configuartion file, within its root section, under the name of `WebApi`

!!! note
	every shard configuration section is mapped by default to the name of the configuration setting class, stripping out the `Settings` part

example:

```json
"WebApi": {
   "EndPoint": "127.0.0.1:45030",
   "Enabled": false,
   "Https": false
}
```