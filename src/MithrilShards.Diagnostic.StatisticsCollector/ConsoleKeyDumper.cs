using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.Statistics;
using MithrilShards.Diagnostic.StatisticsCollector.Models;

namespace MithrilShards.Diagnostic.StatisticsCollector;

public class ConsoleKeyDumper(
   IHostApplicationLifetime hostApplicationLifetime,
   IStatisticFeedsCollector statisticFeedsCollector)
{
   private bool _isRunning;

   public async Task StartListeningAsync()
   {
      if (_isRunning)
      {
         return;
      }

      _isRunning = true;

      while (!hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
      {
         if (DumpKeyPressed())
         {
            IEnumerable<StatisticFeedDefinition> feeds = statisticFeedsCollector.GetRegisteredFeedsDefinitions();

            Console.Clear();

            foreach (var feed in feeds)
            {
               var feedResult = statisticFeedsCollector.GetFeedDump(feed.FeedId, true);
               if (feedResult is TabularStatisticFeedResult tabularResult)
               {
                  Console.WriteLine(tabularResult.Content);
               }
            }
         }

         await Task.Delay(500, hostApplicationLifetime.ApplicationStopping).ConfigureAwait(false);
      }
   }

   private static bool DumpKeyPressed()
   {
      bool found = false;
      while (Console.KeyAvailable)
      {
         if (Console.ReadKey(true).Key == ConsoleKey.S)
         {
            found = true; //don't stop, keep consuming inputs
         }
      }

      return found;
   }
}
