---
title: WEB API Areas
description: Mithril Shards, WebApiShard, WEB API Areas
---

--8<-- "refs.txt"

WebApiShard organize controllers assigning them to specific areas.  

Areas are used to subdivide APIs based on grouping criteria and each area can be enabled or disabled by configuring the corresponding [ApiServiceDefinition].  
By default two areas are defined:

```c#
/// <summary>
/// Placeholder to define known core WEB API areas.
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
forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>((hostBuildContext, services) =>
{
   services.AddSingleton<ApiServiceDefinition>(sp =>
   {
      var settings = sp.GetService<IOptions<DevControllerSettings>>()!.Value;

      var definition = new ApiServiceDefinition
      {
         Enabled = settings.Enabled,
         Area = WebApiArea.AREA_DEV,
         Name = "Dev API",
         Description = "API useful for debug purpose.",
         Version = "v1",
      };

      forgeBuilder.AddApiService(definition);

      return definition;
   });
});
```

In this example you can see that `Area` is set to `WebApiArea.AREA_DEV` that's simply a constant string that's the equivalent of set it to `"dev"` and Enabled is set to `settings.Enabled`, this way it's enabled or disabled based on the `DevControllerSettings` configuration.



### Creating custom areas

The process to create a custom area is the same as the one shown in the example above, the only difference is the `Area` value, that can be any string.

In order to have controllers assigned to such area, a controller has to be decorated with an Area attribute like in this example:

```c#
[Area(WebApiArea.AREA_DEV)]
public class PeerManagementController : MithrilControllerBase
```

You can find more information on controller creation in the [Creating a Controller] section.