using Microsoft.AspNetCore.Mvc;

namespace MithrilShards.WebApi;

[ApiController]
[Produces("application/json")]
[Route("[area]/[controller]/[action]")]
public abstract class MithrilControllerBase : ControllerBase { }
