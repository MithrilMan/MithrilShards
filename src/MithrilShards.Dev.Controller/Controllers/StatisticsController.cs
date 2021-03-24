using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Statistics;
using MithrilShards.Dev.Controller.Models.Responses;
using MithrilShards.Diagnostic.StatisticsCollector.Models;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller.Controllers
{
   [Area(WebApiArea.AREA_DEV)]
   [TypeFilter(typeof(StatisticOnlyActionFilterAttribute))]
   public class StatisticsController : MithrilControllerBase
   {
      private readonly ILogger<StatisticsController> _logger;
      // StatisticOnlyActionFilterAttribute will prevent to use actions in this controller if StatisticFeedsCollector isn't resolved
      readonly IStatisticFeedsCollector _statisticFeedsCollector = null!;

      public StatisticsController(ILogger<StatisticsController> logger, IStatisticFeedsCollector statisticFeedsCollector)
      {
         _logger = logger;
         _statisticFeedsCollector = statisticFeedsCollector!;
      }

      /// <summary>
      /// Gets all the statistic feeds in a raw JSON format.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetStats()
      {
         return Ok(_statisticFeedsCollector.GetFeedsDump());
      }

      /// <summary>
      /// Gets the specified statistic feed.
      /// </summary>
      /// <param name="feedId">The feed identifier.</param>
      /// <param name="humanReadable">if set to <c>true</c> prints a human readable output, otherwise returns a JSON raw feed.</param>
      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetFeedStats(string feedId, bool humanReadable)
      {
         try
         {
            return _statisticFeedsCollector.GetFeedDump(feedId, humanReadable) switch
            {
               RawStatisticFeedResult result => Ok(result),
               TabularStatisticFeedResult result => Content(result.Content, "text/plain"),
               IStatisticFeedResult result => Ok(result),
               _ => NotFound()
            };
         }
         catch (System.ArgumentException)
         {
            return NotFound($"Feed {feedId} not found!");
         }
      }

      /// <summary>
      /// Gets the available feeds.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IEnumerable<StatisticsGetAvailableFeeds> GetAvailableFeeds()
      {
         return _statisticFeedsCollector.GetRegisteredFeedsDefinitions()
            .Select(feed => new StatisticsGetAvailableFeeds
            {
               FeedId = feed.FeedId,
               Title = feed.Title,
               Fields = feed.FieldsDefinition.Select(field => new StatisticsGetAvailableFeeds.StatisticFeedField { Label = field.Label, Description = field.Description }).ToList()
            });
      }
   }
}
