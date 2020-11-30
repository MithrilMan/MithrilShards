using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MithrilShards.WebApi.Filters.OperationFilters
{
   public class AuthResponsesOperationFilter : IOperationFilter
   {
      public void Apply(OpenApiOperation operation, OperationFilterContext context)
      {
         var authAttributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
             .Union(context.MethodInfo.GetCustomAttributes(true))
             .OfType<AuthorizeAttribute>();

         if (authAttributes.Any())
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
      }
   }
}
