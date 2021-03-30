---
title: Web API Areas
description: Mithril Shards, WebApiShard, Web API Areas
---

--8<-- "refs.txt"

WebApiShard organize controllers assigning them to specific areas.  

Areas are used to subdivide APIs based on grouping criteria and each area can be enabled or disabled by configuring the corresponding [ApiServiceDefinition].  
By default two areas are defined:

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



Custom areas can be created easily by defining them using [ApiServiceDefinition].  
[DevControllerShard] for example defines its own [ApiServiceDefinition] to group all controllers meant to be used for development / diagnostic purpose.

!!! note
	Any WebApiShard compliant controller has to belong to a specific area, by specifying an `AreaAttribute` at controller class level.



## ApiServiceDefinition

ApiServiceDefinition is a class responsible to hold a WEB Api area configuration.  
It contains an `Enabled` property used to enable or disable that specific area and its value is usually set by using the configuration file of the shard responsible for the ApiServiceDefinition.

[DevControllerShard] code shows an example of how to register an area during the shard registration:

```c#
forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>((context, services) =>
{
   if (context.GetShardSettings<DevControllerSettings>()!.Enabled)
   {
      services.AddApiServiceDefinition(new ApiServiceDefinition
      {
         Area = WebApiArea.AREA_DEV,
         Name = "Dev API",
         Description = "API useful for debug purpose.",
         Version = "v1",
      });
   }
});
```

In this example you can see that `Area` is set to `WebApiArea.AREA_DEV` that's simply a constant string that's the equivalent of set it to `"dev"` and Enabled is set to `settings.Enabled`, this way it's enabled or disabled based on the `DevControllerSettings` configuration.

!!! note
	Each different ApiServiceDefinition generates an OpenAPI document following its specification.
	Swagger UI allows to select which document to show, see [Using Swagger UI] section.
	An OpenAPI document can be used by tools like [AutoRest](https://github.com/Azure/autorest){:target="_blank"} to generate automatically clients for RESTful API, not just for C# but for many other languages (after all OpenAPI is an agnostic specification).



### Creating custom areas

The process to create a custom area is the same as the one shown in the example above, the only difference is the `Area` value, that can be any string.

If we want to create an area named "area51" and be available for controllers defined in our shards or a 3rd party shards, we can register such area by creating a new ApiServiceDefinition instance and register it using AddApiServiceDefinition:

```c#
forgeBuilder.AddShard<YourShard, YourShardSettings>((context, services) =>
{
   if (context.GetShardSettings<YourShardSettings>()!.Enabled)
   {
      services.AddApiServiceDefinition(new ApiServiceDefinition
      {
         Enabled = true,
         Area = "area51",
         Name = "Area 51 - trust no one!",
         Description = "Nothing to see here...",
         Version = "v1",
      });
   }
});
```

The example above implies that YourShardSettings has a `Enabled` boolean property that describes if the shard has to generate or not an ApiServiceDefinition. You are free to skip that check if you want to have an area always defined and/or want to tweak the ApiServiceDefinition Enabled property at runtime based on some custom conditions.

!!! note
	An ApiServiceDefinition can be enabled or disabled at runtime by changing its `Enabled` property.  
	Registering an area implicitly generates its OpenAPI document, but access to its API are controlled by its Enabled property.



In order to have controllers assigned to such area, a controller has to be decorated with an Area attribute like in this example:

```c#
[Area("area51")]
public class YourAreaController : MithrilControllerBase
```

You can find more information on controller creation in the [Creating a Controller] section.