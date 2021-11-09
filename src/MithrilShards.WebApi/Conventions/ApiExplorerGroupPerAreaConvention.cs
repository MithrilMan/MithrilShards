using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MithrilShards.WebApi.Conventions;

public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention
{
   public void Apply(ControllerModel controller)
   {
      if (controller.ControllerType.IsAbstract) return; //we are only interested in concrete Controllers

      controller.RouteValues.TryGetValue("area", out string? area);
      controller.ApiExplorer.GroupName = area;
   }
}
