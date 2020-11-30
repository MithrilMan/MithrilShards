using Microsoft.AspNetCore.Mvc;

namespace MithrilShards.WebApi
{
   [ApiController]
   [Produces("application/json")]
   [Route("[area]/[controller]")]
   public class MithrilControllerBase : ControllerBase { }
}
