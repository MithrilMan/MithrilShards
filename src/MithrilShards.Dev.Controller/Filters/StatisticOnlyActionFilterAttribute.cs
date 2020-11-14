using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Dev.Controller
{
   public class StatisticOnlyActionFilterAttribute : ActionFilterAttribute
   {
      readonly IStatisticFeedsCollector? statisticFeedsCollector;

      public StatisticOnlyActionFilterAttribute(IStatisticFeedsCollector? statisticFeedsCollector = null)
      {
         this.statisticFeedsCollector = statisticFeedsCollector;
      }

      public override void OnActionExecuting(ActionExecutingContext context)
      {
         if (this.statisticFeedsCollector == null || this.statisticFeedsCollector is StatisticFeedsCollectorNullImplementation)
         {
            context.Result = new NotFoundObjectResult($"Cannot produce output because {nameof(IStatisticFeedsCollector)} is not available");
            return;
         }

         base.OnActionExecuting(context);
      }
   }
}
