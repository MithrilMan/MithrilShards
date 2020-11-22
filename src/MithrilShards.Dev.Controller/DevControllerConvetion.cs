using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerConvetion : IControllerModelConvention
   {
      private const string DEV_CONTROLLER_TYPE_NAME_SUFFIX = "ControllerDev";

      public void Apply(ControllerModel controller)
      {
         TypeInfo typeInfo = controller.ControllerType;

         if (controller.ControllerType.Name.EndsWith(DEV_CONTROLLER_TYPE_NAME_SUFFIX))
         {
            controller.ControllerName =
                typeInfo.Name.EndsWith(DEV_CONTROLLER_TYPE_NAME_SUFFIX, StringComparison.OrdinalIgnoreCase) ?
                    typeInfo.Name.Substring(0, typeInfo.Name.Length - DEV_CONTROLLER_TYPE_NAME_SUFFIX.Length) :
                    typeInfo.Name;
         }
      }
   }
}