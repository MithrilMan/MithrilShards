using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Diagnostic.StatisticsCollector;

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [TypeFilter(typeof(StatisticOnlyActionFilterAttribute))]
   [Route("[controller]")]
   public class StatisticsController : ControllerBase
   {
      private readonly ILogger<PeerManagementController> logger;
      // StatisticOnlyActionFilterAttribute will prevent to use actions in this controller if StatisticFeedsCollector isn't resolved
      readonly StatisticFeedsCollector statisticFeedsCollector = null!;

      public StatisticsController(ILogger<PeerManagementController> logger, StatisticFeedsCollector? statisticFeedsCollector = null)
      {
         this.logger = logger;
         this.statisticFeedsCollector = statisticFeedsCollector!;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetStats()
      {
         return this.Ok(this.statisticFeedsCollector.GetFeedsDump());
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [Route("{feedId}")]
      public IActionResult GetStats(string feedId, bool humanReadable)
      {
         return this.Ok(this.statisticFeedsCollector.GetFeedDump(feedId, humanReadable));
      }

      //[HttpGet]
      //[ProducesResponseType(StatusCodes.Status200OK)]
      //[ProducesResponseType(StatusCodes.Status404NotFound)]
      //[Route("AvailableFeeds")]
      //public IActionResult GetAvailableFeeds()
      //{
      //   return this.Ok(this.statisticFeedsCollector.GetFeedDump(feedId, humanReadable));
      //}
   }
}
