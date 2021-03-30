---
title: Overview
description: Mithril Shards, WebApiShard Overview
---
--8<-- "refs.txt"

WebApiShard is an important shard that allows to expose Web API endpoints based on [OpeAPI specifications](https://swagger.io/specification/){:target="_blank"}.

[Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore){:target="_blank"} is used under the hood and you can find more technical information about OpenAPI, REST APIs and Swagger concepts on [microsoft documentation](https://docs.microsoft.com/it-it/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-5.0){:target="_blank"}.

WebApiShard comes with a [WebApiSettings] class that holds settings to configure the service.

To add the shard to the forge, the `IForgeBuilder` extension `UseApi` has to be called, passing optional `options` to further configure the service

```c#
public static IForgeBuilder UseApi(this IForgeBuilder forgeBuilder, Action<WebApiOptions>? options = null)
```

WebApiShard implements the Web API controllers using the standard aspnet [ControllerBase](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase?view=aspnetcore-5.0){:target="_blank"} class but decorates it with a set of default attributes needed to expose these controllers in the right context.
To create a proper WebApiShard controller, an abstract base class `MithrilControllerBase` exists that applies already the required attributes.

```c#
[ApiController]
[Produces("application/json")]
[Route("[area]/[controller]/[action]")]
public abstract class MithrilControllerBase : ControllerBase { }
```

Areas are used to subdivide APIs based on grouping criteria and each area can be enabled or disabled by configuring the corresponding [ApiServiceDefinition].  
By default two areas are defined but custom areas can be created easily.

```c#
/// <summary>
/// Placeholder to define known core Web API areas.
/// This class may be extended to add more const for 3rd party areas.
/// </summary>
public abstract class WebApiArea
{
   /// <summary>
   /// The default API area where common actions will be available.
   /// </summary>
   public const string AREA_API = "api";

   /// <summary>
   /// The area where Dev controllers has to be placed.
   /// Dev controllers are controllers useful during debug that can expose internal details
   /// or are risky to be used in a public environment
   /// They may be risky to execute by an end user that doesn't have good technical details
   /// knowledge about the application.
   /// </summary>
   public const string AREA_DEV = "dev";
}
```

!!! note
	WebApiShard controllers have to belong to a specific area. More information in [Creating a Controller] section.

`DisableByEndPointActionFilterAttribute` class, that's a registered [ActionFilterAttribute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.filters.actionfilterattribute?view=aspnetcore-5.0){:target="_blank"}, is responsible to enforce proper checks against executing an action on an unspecified, unknown or disabled area.

!!! warning
	The current implementation may be subject to changes to implement the authentication and authorization layer.