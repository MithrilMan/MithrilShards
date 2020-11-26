using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Statistics;
using MithrilShards.Dev.Controller;
using Serilog.Core;
using Serilog.Events;

namespace MithrilShards.Logging.Serilog.Controllers
{
   [ApiController]
   [DevController]
   [Route("[controller]")]
   public class SeriLogController : ControllerBase
   {
      private readonly ILogger<SeriLogController> _logger;
      readonly LevelSwitcherManager _levelSwitcherManager;

      public SeriLogController(ILogger<SeriLogController> logger, LevelSwitcherManager levelSwitcherManager)
      {
         _logger = logger;
         _levelSwitcherManager = levelSwitcherManager;
      }

      [HttpGet("Levels")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public IActionResult GetCurrentLevels()
      {
         return Ok(_levelSwitcherManager.GetCurrentLevels());
      }

      [HttpGet("Level/{context}")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetCurrentLevel(string context)
      {
         LoggingLevelSwitch? entry = _levelSwitcherManager.GetCurrentLevel(context);
         if (entry == null)
         {
            return NotFound();
         }
         else
         {
            return Ok(entry);
         }
      }

      [HttpPut("Level/{context}")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult SetCurrentLevel(string context, LogEventLevel level)
      {
         if (_levelSwitcherManager.SetLevel(context, level))
         {
            return Ok(_levelSwitcherManager.GetCurrentLevel(context));
         }
         else
         {
            return NotFound();
         }
      }
   }
}