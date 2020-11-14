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

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class PeerManagementControllerDev : ControllerBase
   {
      private readonly ILogger<PeerManagementControllerDev> logger;
      readonly IEventBus eventBus;
      readonly RequiredConnection? requiredConnection;

      public PeerManagementControllerDev(ILogger<PeerManagementControllerDev> logger, IEventBus eventBus, IEnumerable<IConnector>? connectors)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.requiredConnection = connectors?.OfType<RequiredConnection>().FirstOrDefault();
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Connect")]
      public ActionResult<bool> Connect(PeerManagementConnectRequest request)
      {
         if (this.requiredConnection == null)
         {
            return this.NotFound($"Cannot produce output because {nameof(RequiredConnection)} is not available");
         }

         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint ipEndPoint))
         {
            return this.BadRequest("Incorrect endpoint");
         }

         return this.Ok(this.requiredConnection.TryAddEndPoint(ipEndPoint));
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Disconnect")]
      public ActionResult<bool> Disconnect(PeerManagementDisconnectRequest request)
      {
         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint ipEndPoint))
         {
            return this.BadRequest("Incorrect endpoint");
         }

         this.eventBus.Publish(new PeerDisconnectionRequired(ipEndPoint, request.Reason));
         return true;
      }
   }
}
