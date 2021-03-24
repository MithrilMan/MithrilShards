using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.WebApi;

namespace MithrilShards.Example.Dev
{
   /// <summary>
   /// An example of a Dev Controller (controllers meant to be exposed to the Dev.Controller shard that exposes endpoints through a dedicated swagger endpoint.
   /// In order to be discoverable by Dev Controller, this controller has to inherit from MithrilControllerBase class name has to end with suffix "ControllerDev" or being decorated with a DevControllerAttribute.
   ///
   /// In this example we are obtaining a reference to IQuoteService in order to be able to add and remove quotes that can be sent as a ping response.
   /// IQuoteService is a service that has been registered in our DI container during the shard registration so we can
   /// just have a reference of it in our constructor and it will be injected automatically during the instantiation of this class.
   /// </summary>
   /// <seealso cref="MithrilControllerBase" />
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

      /// <summary>
      /// Gets the available quotes.
      /// </summary>
      /// <returns></returns>
      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
      public ActionResult GetQuotes()
      {
         return Ok(_quoteService.Quotes);
      }

      /// <summary>
      /// Adds a quote.
      /// </summary>
      /// <param name="quote">The quote.</param>
      /// <returns></returns>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public ActionResult AddQuote(string quote)
      {
         _quoteService.Quotes.Add(quote);

         _logger.LogDebug("A new quote has been added: `{Quote}`", quote);

         return Ok($"Quote `{quote}` added.");
      }

      /// <summary>
      /// Removes a quote.
      /// </summary>
      /// <param name="quoteIndex">Index of the quote to be removed.</param>
      /// <returns></returns>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
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

      /// <summary>
      /// Clears the quotes, removing all entries.
      /// </summary>
      /// <returns></returns>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public ActionResult ClearQuotes()
      {
         _quoteService.Quotes.Clear();
         return Ok();
      }
   }
}
