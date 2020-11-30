using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MithrilShards.WebApi.Conventions
{
   public class ApiExplorerGetsOnlyProperAreaConvention : IActionModelConvention
   {
      public void Apply(ActionModel action)
      {
         action.ApiExplorer.IsVisible = action.Attributes.OfType<HttpGetAttribute>().Any();
      }
   }
}