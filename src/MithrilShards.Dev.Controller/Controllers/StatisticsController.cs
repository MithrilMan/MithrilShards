using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Statistics;
using MithrilShards.Dev.Controller.Models.Responses;
using MithrilShards.Diagnostic.StatisticsCollector.Models;

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [DevController]
   [TypeFilter(typeof(StatisticOnlyActionFilterAttribute))]
   [Route("[controller]")]
   public class StatisticsController : ControllerBase
   {
      private readonly ILogger<StatisticsController> _logger;
      // StatisticOnlyActionFilterAttribute will prevent to use actions in this controller if StatisticFeedsCollector isn't resolved
      readonly IStatisticFeedsCollector _statisticFeedsCollector = null!;

      public StatisticsController(ILogger<StatisticsController> logger, IStatisticFeedsCollector statisticFeedsCollector)
      {
         this._logger = logger;
         this._statisticFeedsCollector = statisticFeedsCollector!;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetStats()
      {
         return this.Ok(this._statisticFeedsCollector.GetFeedsDump());
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [Route("{feedId}")]
      public IActionResult GetFeedStats(string feedId, bool humanReadable)
      {
         try
         {
            return this._statisticFeedsCollector.GetFeedDump(feedId, humanReadable) switch
            {
               RawStatisticFeedResult result => this.Ok(result),
               TabularStatisticFeedResult result => this.Content(result.Content, "text/plain"),
               IStatisticFeedResult result => this.Ok(result),
               _ => this.NotFound()
            };
         }
         catch (System.ArgumentException)
         {
            return this.NotFound($"Feed {feedId} not found!");
         }
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [Route("AvailableFeeds")]
      public IEnumerable<StatisticsGetAvailableFeeds> GetAvailableFeeds()
      {
         return this._statisticFeedsCollector.GetRegisteredFeedsDefinitions()
            .Select(feed => new StatisticsGetAvailableFeeds
            {
               FeedId = feed.FeedId,
               Title = feed.Title,
               Fields = feed.FieldsDefinition.Select(field => new StatisticsGetAvailableFeeds.StatisticFeedField { Label = field.Label, Description = field.Description }).ToList()
            });
      }
   }
}
