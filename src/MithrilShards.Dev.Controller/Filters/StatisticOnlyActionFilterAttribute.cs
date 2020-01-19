using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MithrilShards.Diagnostic.StatisticsCollector;

namespace MithrilShards.Dev.Controller
{
   public class StatisticOnlyActionFilterAttribute : ActionFilterAttribute
   {
      readonly StatisticFeedsCollector? statisticFeedsCollector;

      public StatisticOnlyActionFilterAttribute(StatisticFeedsCollector? statisticFeedsCollector = null)
      {
         this.statisticFeedsCollector = statisticFeedsCollector;
      }

      public override void OnActionExecuting(ActionExecutingContext context)
      {
         if (this.statisticFeedsCollector == null)
         {
            context.Result = new NotFoundObjectResult($"Cannot produce output because {nameof(StatisticFeedsCollector)} is not available");
            return;
         }

         base.OnActionExecuting(context);
      }
   }
}
