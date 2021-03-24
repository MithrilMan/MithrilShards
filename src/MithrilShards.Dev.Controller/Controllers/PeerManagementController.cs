using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Dev.Controller.Models.Requests;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller.Controllers
{
   [Area(WebApiArea.AREA_DEV)]
   public class PeerManagementController : MithrilControllerBase
   {
      private readonly ILogger<PeerManagementController> _logger;
      readonly IEventBus _eventBus;
      readonly RequiredConnection? _requiredConnection;

      public PeerManagementController(ILogger<PeerManagementController> logger, IEventBus eventBus, IEnumerable<IConnector>? connectors)
      {
         _logger = logger;
         _eventBus = eventBus;
         _requiredConnection = connectors?.OfType<RequiredConnection>().FirstOrDefault();
      }

      /// <summary>
      /// Adds a connection request, trying to connect to the specified endpoint.
      /// </summary>
      /// <param name="request">The request.</param>
      /// <returns></returns>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public IActionResult Connect(PeerManagementConnectRequest request)
      {
         if (_requiredConnection == null)
         {
            return ValidationProblem($"Cannot produce output because {nameof(RequiredConnection)} is not available");
         }

         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint? ipEndPoint))
         {
            return ValidationProblem("Incorrect endpoint");
         }

         _requiredConnection.TryAddEndPoint(ipEndPoint);
         return Ok();
      }

      /// <summary>
      /// Tries to disconnects from the specified endpoint.
      /// </summary>
      /// <param name="request">The request.</param>
      /// <returns></returns>
      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public IActionResult Disconnect(PeerManagementDisconnectRequest request)
      {
         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint? ipEndPoint))
         {
            return ValidationProblem("Incorrect endpoint");
         }

         _eventBus.Publish(new PeerDisconnectionRequired(ipEndPoint, request.Reason));
         return Ok();
      }
   }
}
