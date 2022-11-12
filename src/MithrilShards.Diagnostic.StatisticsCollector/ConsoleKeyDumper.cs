using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.Statistics;
using MithrilShards.Diagnostic.StatisticsCollector.Models;

namespace MithrilShards.Diagnostic.StatisticsCollector;

public class ConsoleKeyDumper
{
   readonly IHostApplicationLifetime _hostApplicationLifetime;
   readonly IStatisticFeedsCollector _statisticFeedsCollector;
   private bool _isRunning;

   public ConsoleKeyDumper(IHostApplicationLifetime hostApplicationLifetime, IStatisticFeedsCollector statisticFeedsCollector)
   {
      _hostApplicationLifetime = hostApplicationLifetime;
      _statisticFeedsCollector = statisticFeedsCollector;
   }

   public async Task StartListening()
   {
      if (_isRunning)
      {
         return;
      }

      _isRunning = true;
      bool consoleInputAvailable = !Console.IsInputRedirected;
      if (consoleInputAvailable)
      {
         Console.WriteLine("No console input available, DumpKeyPressed disabled.");
      }

      while (!_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
      {
         if (consoleInputAvailable && DumpKeyPressed())
         {
            IEnumerable<StatisticFeedDefinition> feeds = _statisticFeedsCollector.GetRegisteredFeedsDefinitions();

            Console.Clear();

            foreach (var feed in feeds)
            {
               var feedResult = _statisticFeedsCollector.GetFeedDump(feed.FeedId, true);
               if (feedResult is TabularStatisticFeedResult tabularResult)
               {
                  Console.WriteLine(tabularResult.Content);
               }
            }
         }

         await Task.Delay(500, _hostApplicationLifetime.ApplicationStopping).ConfigureAwait(false);
      }
   }

   private bool DumpKeyPressed()
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
