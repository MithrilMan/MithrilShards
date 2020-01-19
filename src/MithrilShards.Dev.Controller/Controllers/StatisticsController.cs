using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Diagnostic.StatisticsCollector;

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class StatisticsController : ControllerBase
   {
      private readonly ILogger<PeerManagementController> logger;
      readonly StatisticFeedsCollector? statisticFeedsCollector;

      public StatisticsController(ILogger<PeerManagementController> logger, StatisticFeedsCollector? statisticFeedsCollector)
      {
         this.logger = logger;
         this.statisticFeedsCollector = statisticFeedsCollector;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetStats()
      {
         if (this.statisticFeedsCollector == null)
         {
            return this.NotFound($"Cannot produce output because {nameof(StatisticsController)} is not available");
         }

         return this.Ok(this.statisticFeedsCollector.GetFeedsDump());
      }
   }
}
