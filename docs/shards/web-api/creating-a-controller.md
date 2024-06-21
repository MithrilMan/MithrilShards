---
title: Creating a Controller
description: Mithril Shards, WebApiShard, Creating a Controller
---

--8<-- "refs.txt"

To create a controller that can be exposed by the WebApiShard we can take advantage of the `MithrilControllerBase` class.

We can take a look at the controller implemented in the [example project], to dissect it and discuss about its implementation.

Let's take a meaningful part of that class and let's dissect it by highlighting some code part:

```c# hl_lines="1 2 7-10 13-18"
[Area(WebApiArea.AREA_API)]
public class ExampleController : MithrilControllerBase
{
   private readonly ILogger<ExampleController> _logger;
   readonly IQuoteService _quoteService;

   public ExampleController(ILogger<ExampleController> logger, IQuoteService quoteService)
   {
      _logger = logger;
      _quoteService = quoteService;
   }

   [HttpGet]
   [ProducesResponseType(StatusCodes.Status200OK)]
   public ActionResult GetQuotes()
   {
      return Ok(_quoteService.Quotes);
   }
```



### Define the area

Line 1 describes, using [AreaAttribute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.areaattribute?view=aspnetcore-8.0){:target="_blank"}, the area we want this controller to be included.

WebApiArea is an helper class that just contains a bounch of constant string of known areas: "api" and "dev".

- "api" ( `WebApiArea.AREA_API` ) is the area where generic purpose controller should be placed.
  They are meant to be used by end users or 3rd party integration, to interact with our application.
- "dev" ( `WebApiArea.AREA_DEV` ) is the area where Dev controllers should be placed.
  Dev controllers are controllers useful during debug that can expose internal details or are risky to be used in a 
  public environment.
  They may be risky to execute by an end user that doesn't have good technical details knowledge about the application and generally you want to enable them when you are developing or you need to collect more information on a running instance of your application.

There are some Controllers that are available out of the box when you use Mithril Shards features, an example is [SerilogShard] that includes SeriLogController that allows to control Log filters at runtime and it's exposed in the "api" area, while many more Controllers are exposed in "dev" area like the ones included by [DevControllerShard].

!!! tip
	You can create custom areas, for more information see [Web API Areas] documentation.

### Declare controller Type

Line 2 is the Controller class definition and it just declare our `ExampleController` class that inherit from MithrilControllerBase.
The name of the controller class is important because by default the actions implemented in the controller will have a route like the one defined by RouteAttribute that decorates the MithrilControllerBase

```c#
[Route("[area]/[controller]/[action]")]
```

- area will be replaced by the are we declared our controller belongs to (e.g. "api").
- controller is the name of the Controller class, stripping out "Controller" part, e.g. ExampleController will become "Example"
- action is the name of the action we can invoke

ExampleController action GetQuotes URL will then become `api/Example/GetQuotes`.
This represents the part of the URL to append to the WebApiShard configured Endpoint, so if we configured it to `127.0.0.1:45030` the complete URL will be `http://127.0.0.1:45020/api/Example/GetQuotes` (or https if we enabled Https).



### Inject services into constructor

Line 7-10 is the Controller constructor.
A controller is created automatically at each web request and the parameters declared in the constructor will be populated by using DI
In this example, `ILogger<ExampleController> logger` gets populated with strongly typed instance of our logger and IQuoteService quoteService with the instance of the concrete implementation of our `IQuoteService` that in our [example project] we registered as a singleton

```c#
.AddSingleton<IQuoteService, QuoteService>()
```

!!! note
	It's important to know the life cycle of our injected service because they may impact performance. If the constructor of a service is slow and that service is defined as Transient (or Scoped) every action will have to wait its completion before being able to perform its job.



### Implement an action

Lines 13-18 declare and implement an action.

In this case, the action is declared as `HttpGet`, this mean that it will only respond to GET requests. If you try to access that action with others HTTP verbs, it will return a status error `405 Method Not Allowed`

`[ProducesResponseType(StatusCodes.Status200OK)]` declare known action status that can be returned by the action.
It's just an helpful attribute useful to produce a better documentation on swagger interface and document definition but the action itself may even generate different statuses. This documentation however don't cover canonical Web API implementation, so [refer to .Net documentation](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/conventions?view=aspnetcore-8.0){:target="_blank"}. to read more about it.

To return an action result, the method `Ok` is invoked, passing the payload (that will be serialized in JSON) as a response.

To document better the result type, we could specify in ProduceResponseTypeAttribute the returned type, it could be helpful for the consumer to know which type of object to receive back and which properties it exposes.
In this example it's a simple list of string but it can be any JSON serializable type:

```c#
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
```



As an additional example, this is the code of a DEV controller action that generates different status based on internal state of the node

```c#
[HttpPost]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public IActionResult Connect(PeerManagementConnectRequest request)
{
   if (_requiredConnection == null)
   {
      return NotFound($"Cannot produce output because {nameof(RequiredConnection)} is not available");
   }

   if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint? ipEndPoint))
   {
      return ValidationProblem("Incorrect endpoint");
   }

   _requiredConnection.TryAddEndPoint(ipEndPoint);
   return Ok();
}
```

In this example, this action will return 404 (not found) if the member variables _requiredConnection isn't set, or 400 (bad request) if the input peer isn't formatted properly as a valid endpoint. If everything goes fine it will instead return 200 (ok).

!!! tip
	In case of action problems, instead of calling BadRequest or Problem method extensions, use `ValidationProblem`, it uses a ValidationProblemDetails response that's consistent with automatic validation error responses, [as stated here](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0#default-badrequest-response){:target="_blank"}.
	In the example above, NotFound could be replaced with ValidationProblem too for a consistent behavior.

### Producing documentation for Swagger UI

In order to produce proper documentation to be shown on Swagger UI, [XML comments within C#](https://docs.microsoft.com/it-it/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-8.0&tabs=visual-studio#xml-comments){:target="_blank"} source can be used but the build process has to generate a documentation file.

The easier way is to edit your project file adding this snippet:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

For Mithril Shards project defined within the Mithril Shard solution folder this is not necessary because that snippet is already defined in `Directory.Build.props` file.

!!! note
	WebApiShard already take care of including documentation files by looking at files with the name of the assembly that contains the controller, with an `.xml` extension.  
	`var xmlFile = $"{assembly.GetName().Name}.xml";`

!!! tip
	Directory.Build.props file is a powerful way to set common project configurations for complex solutions with multiple projects, [you can read more about it here](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2019){:target="_blank"}.