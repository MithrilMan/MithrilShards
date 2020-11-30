using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller
{
   /// <summary>
   /// This action prevent to run controllers whose Area has been disabled.
   /// Area can be disabled by passing <see cref="ApiServiceDefinition.Enabled"/> = <see langword="false"/> or changing
   /// its value at runtime, to the <see cref="ApiServiceDefinition"/> instance having <see cref="ApiServiceDefinition.Area"/>
   /// equal to the controller we are about to execute.
   /// </summary>
   /// <remarks>
   /// All controllers must belong to an area.
   /// </remarks>
   /// <seealso cref="Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute" />
   public class DisableByEndPointActionFilterAttribute : ActionFilterAttribute
   {
      readonly ILogger<DisableByEndPointActionFilterAttribute> _logger;
      readonly IEnumerable<ApiServiceDefinition> _apiServiceDefinitions;

      public DisableByEndPointActionFilterAttribute(ILogger<DisableByEndPointActionFilterAttribute> logger, IEnumerable<ApiServiceDefinition> apiServiceDefinitions)
      {
         _logger = logger;
         _apiServiceDefinitions = apiServiceDefinitions;
      }

      public override void OnActionExecuting(ActionExecutingContext context)
      {
         var area = (string)context.RouteData.Values["area"];

         ApiServiceDefinition? apiAreaDefinition = _apiServiceDefinitions.FirstOrDefault(definition => definition.Area == area);
         if (area == null)
         {
            context.Result = new ObjectResult($"Cannot execute the required action because the controller doesn't belong to an Area.") { StatusCode = StatusCodes.Status501NotImplemented };
            return;
         }
         else if (apiAreaDefinition == null)
         {
            context.Result = new ObjectResult($"Cannot execute the required action because the controller belong to an unknown Area.") { StatusCode = StatusCodes.Status501NotImplemented };
            return;
         }
         else if (apiAreaDefinition.Enabled == false)
         {
            context.Result = new ObjectResult($"Cannot execute the required action because the API service {apiAreaDefinition.Area} is disabled.") { StatusCode = StatusCodes.Status501NotImplemented };
            return;
         }

         base.OnActionExecuting(context);
      }
   }
}
