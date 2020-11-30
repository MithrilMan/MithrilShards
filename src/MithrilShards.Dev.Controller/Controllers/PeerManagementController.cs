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

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Connect")]
      public ActionResult<bool> Connect(PeerManagementConnectRequest request)
      {
         if (_requiredConnection == null)
         {
            return NotFound($"Cannot produce output because {nameof(RequiredConnection)} is not available");
         }

         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint ipEndPoint))
         {
            return BadRequest("Incorrect endpoint");
         }

         return Ok(_requiredConnection.TryAddEndPoint(ipEndPoint));
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Disconnect")]
      public ActionResult<bool> Disconnect(PeerManagementDisconnectRequest request)
      {
         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint ipEndPoint))
         {
            return BadRequest("Incorrect endpoint");
         }

         _eventBus.Publish(new PeerDisconnectionRequired(ipEndPoint, request.Reason));
         return true;
      }
   }
}
