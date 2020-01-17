using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class ForgeController : ControllerBase
   {
      private readonly ILogger<ForgeController> logger;

      public ForgeController(ILogger<ForgeController> logger)
      {
         this.logger = logger;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status201Created)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public ActionResult<bool> Test()
      {
         return true;
      }
   }
}
