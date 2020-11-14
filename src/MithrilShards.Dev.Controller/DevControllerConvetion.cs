using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerConvetion : IControllerModelConvention
   {
      private const string DevControllerTypeNameSuffix = "ControllerDev";

      public void Apply(ControllerModel controller)
      {
         TypeInfo typeInfo = controller.ControllerType;

         if (controller.ControllerType.Name.EndsWith(DevControllerTypeNameSuffix))
         {
            controller.ControllerName =
                typeInfo.Name.EndsWith(DevControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) ?
                    typeInfo.Name.Substring(0, typeInfo.Name.Length - DevControllerTypeNameSuffix.Length) :
                    typeInfo.Name;
         }
      }
   }
}