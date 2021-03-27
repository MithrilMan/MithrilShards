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
      /// <response code="200">The list of known quotes.</response>
      /// <remarks>
      /// Returns a list of all known quotes.
      /// </remarks>
      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
      public ActionResult GetQuotes()
      {
         return Ok(_quoteService.Quotes);
      }

      /// <summary>
      /// Adds a quote.
      /// </summary>
      /// <param name="quote" example="Tu quoque, Brute, fili mi!">The quote to add.</param>
      /// <response code="200">The quote that has been added.</response>
      /// <remarks>
      /// Adds a quote to the quotes list.
      /// </remarks>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public ActionResult AddQuote(string quote)
      {
         _quoteService.Quotes.Add(quote);

         _logger.LogDebug("A new quote has been added: `{Quote}`", quote);

         return Ok(quote);
      }

      /// <summary>Removes a quote at the specified index position.</summary>
      /// <param name="quoteIndex" example="3">Index of the quote to be removed.</param>
      /// <response code="200">The quote that has been removed.</response>
      /// <remarks>
      /// Attempts to remove an entry from the available quotes at the index (0 based) specified by quoteIndex parameter.
      /// If the operation succeeds, the removed quote is returned, otherwise a status 400 error is generated
      /// with the failure reason (ex. index out of range)
      /// </remarks>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public ActionResult RemoveQuote([Range(1, int.MaxValue)] int quoteIndex)
      {
         if (_quoteService.Quotes.Count > quoteIndex)
         {
            string removedQuote = _quoteService.Quotes[quoteIndex];
            if (_quoteService.Quotes.Remove(removedQuote))
            {
               _logger.LogDebug("Quote `{RemovedQuote}` removed.", removedQuote);
               return Ok(removedQuote);
            }
            else
            {
               return ValidationProblem($"Error while removing quote at index {quoteIndex}.");
            }
         }
         else
         {
            return ValidationProblem($"Quote index out of range, available quotes: {_quoteService.Quotes.Count}.");
         }
      }

      /// <summary>
      /// Clears the quotes, removing all entries.
      /// </summary>
      /// <remarks>Removes all the quotes.</remarks>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      public ActionResult ClearQuotes()
      {
         _quoteService.Quotes.Clear();
         return Ok();
      }
   }
}
