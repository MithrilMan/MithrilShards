using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Example.Protocol.Processors;

namespace MithrilShards.Example.Dev
{
   /// <summary>
   /// An example of a Dev Controller (controllers meant to be exposed to the Dev.Controller shard that exposes endpoints through a dedicated swagger endpoint.
   /// In order to be discoverable by Dev Controller, this controller class name has to end with suffix "ControllerDev" or being decorated with a DevControllerAttribute.
   ///
   /// In this example we are obtaining a reference to our PingPongProcessor in order to be able to add and remove quotes that can be sent as a ping response.
   /// PingPongProcessor is a processor that we registered thanks to assembly scaffolding in <see cref="ForgeBuilderExtensions.AddMessageProcessors"/> so we can
   /// just have a reference of it in our constructor and it will be injected automatically during the instantiation of this class.
   /// </summary>
   /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
   [ApiController]
   [Route("[controller]")]
   public class ExampleControllerDev : ControllerBase
   {
      private readonly ILogger<ExampleControllerDev> _logger;
      readonly IQuoteService _quoteService;

      public ExampleControllerDev(ILogger<ExampleControllerDev> logger, IQuoteService quoteService)
      {
         _logger = logger;
         _quoteService = quoteService;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("GetQuotes")]
      public ActionResult GetQuotes()
      {
         return Ok(_quoteService.Quotes);
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("AddQuote")]
      public ActionResult AddQuote(string quote)
      {
         _quoteService.Quotes.Add(quote);

         _logger.LogDebug("A new quote has been added to {DevController}: `{Quote}`", nameof(PingPongProcessor), quote);

         return Ok($"Quote `{quote}` added.");
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("RemoveQuote")]
      public ActionResult RemoveQuote([Range(1, int.MaxValue)] int quoteIndex)
      {
         if (_quoteService.Quotes.Count > quoteIndex)
         {
            string removedQuote = _quoteService.Quotes[quoteIndex];
            if (_quoteService.Quotes.Remove(removedQuote))
            {
               return Ok($"Quote `{removedQuote}` removed.");
            }
            else
            {
               return Problem($"Error while removing quote at index {quoteIndex}.");
            }
         }
         else
         {
            return BadRequest($"Quote index out of range, available quotes: {_quoteService.Quotes.Count}.");
         }
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("ClearQuotes")]
      public ActionResult ClearQuotes()
      {
         _quoteService.Quotes.Clear();
         return Ok();
      }

   }
}
