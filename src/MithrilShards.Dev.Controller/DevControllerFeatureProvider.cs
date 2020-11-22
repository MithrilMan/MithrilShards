using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace MithrilShards.Dev.Controller
{
   public class DevControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
   {
      private const string DevControllerTypeNameSuffix = "ControllerDev";

      public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
      {
         foreach (IApplicationPartTypeProvider? part in parts.OfType<IApplicationPartTypeProvider>())
         {
            foreach (TypeInfo? type in part.Types)
            {
               if (IsDevController(type) && !feature.Controllers.Contains(type))
               {
                  feature.Controllers.Add(type);
               }
            }
         }
      }


      /// <summary>
      /// Determines if a given <paramref name="typeInfo"/> is a controller.
      /// </summary>
      /// <param name="typeInfo">The <see cref="TypeInfo"/> candidate.</param>
      /// <returns><see langword="true" /> if the type is a dev controller; otherwise <see langword="false" />.</returns>
      protected virtual bool IsDevController(TypeInfo typeInfo)
      {
         if (!typeInfo.IsClass)
         {
            return false;
         }

         if (typeInfo.IsAbstract)
         {
            return false;
         }

         // We only consider public top-level classes as controllers. IsPublic returns false for nested
         // classes, regardless of visibility modifiers
         if (!typeInfo.IsPublic)
         {
            return false;
         }

         if (typeInfo.ContainsGenericParameters)
         {
            return false;
         }

         if (!typeInfo.Name.EndsWith(DevControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase) &&
             !typeInfo.IsDefined(typeof(DevControllerAttribute)))
         {
            return false;
         }

         return true;
      }
   }
}
