using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Statistics;
using MithrilShards.Logging.ConsoleTableFormatter;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class StatisticFeedsCollector : IHostedService, IStatisticFeedsCollector
   {
      readonly List<ScheduledStatisticFeed> registeredfeedDefinitions = new List<ScheduledStatisticFeed>();
      readonly ILogger<StatisticFeedsCollector> logger;
      readonly OutputWriter writer;
      private readonly object statisticsFeedsLock = new object();
      private readonly StringBuilder logStringBuilder;

      public StatisticFeedsCollector(ILogger<StatisticFeedsCollector> logger)
      {
         this.logger = logger;
         this.writer = new OutputWriter(text => this.logStringBuilder.Append(text)); // writer;

         this.logStringBuilder = new StringBuilder();
      }

      public void RegisterStatisticFeeds(IStatisticFeedsProvider statisticSource, params StatisticFeedDefinition[] statisticfeeds)
      {
         lock (this.statisticsFeedsLock)
         {
            if (statisticfeeds != null)
            {
               foreach (StatisticFeedDefinition feed in statisticfeeds)
               {
                  this.registeredfeedDefinitions.Add(new ScheduledStatisticFeed(statisticSource, feed));
               }
            }
         }
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         _ = this.StartFetchingLoopAsync(cancellationToken);

         return Task.CompletedTask;
      }

      public async Task StartFetchingLoopAsync(CancellationToken cancellationToken)
      {
         try
         {
            while (!cancellationToken.IsCancellationRequested)
            {
               lock (this.statisticsFeedsLock)
               {
                  DateTimeOffset currentTime = DateTimeOffset.Now;
                  foreach (ScheduledStatisticFeed feed in this.registeredfeedDefinitions)
                  {
                     if (feed.NextPlannedExecution <= currentTime)
                     {
                        StatisticFeedDefinition feedDefinition = feed.StatisticFeedDefinition;
                        feed.NextPlannedExecution += feedDefinition.FrequencyTarget;

                        try
                        {
                           if (feed.TableBuilder == null)
                           {
                              feed.TableBuilder = this.CreateTableBuilder(feedDefinition);
                           }

                           feed.TableBuilder.Start(feedDefinition.Title);

                           var newValues = new List<string?[]>();

                           List<object[]>? statisticValues = feed.Source.GetStatisticFeedValues(feedDefinition.FeedId);
                           if (statisticValues != null)
                           {
                              foreach (object[] values in statisticValues)
                              {
                                 for (int i = 0; i < feedDefinition.FieldsDefinition.Count; i++)
                                 {
                                    FieldDefinition field = feedDefinition.FieldsDefinition[i];
                                    // apply formatting if needed
                                    if (field.ValueFormatter != null)
                                    {
                                       values[i] = field.ValueFormatter(values[i]);
                                    }
                                 }

                                 string?[] formattedValues = feedDefinition.FieldsDefinition
                                    .Select((field, index) => field.ValueFormatter == null ? values[index].ToString() : field.ValueFormatter(values[index]))
                                    .ToArray();

                                 newValues.Add(formattedValues);

                                 feed.TableBuilder.DrawRow(formattedValues);
                              }
                           }
                           feed.TableBuilder.End();

                           feed.LastResults.Clear();
                           feed.LastResults.AddRange(newValues);
                        }
                        catch (Exception ex)
                        {
                           this.logger.LogDebug(ex, "Error generating statistics for {IStatisticFeedsProvider}", feed.Source.GetType().Name);
                           throw;
                        }
                     }
                  }
               }

               if (this.logStringBuilder.Length > 0)
               {
                  Console.WriteLine(this.logStringBuilder.ToString());
                  this.logStringBuilder.Clear();
               }

               await Task.Delay(TimeSpan.FromSeconds(5)).WithCancellationAsync(cancellationToken).ConfigureAwait(false);
            }
         }
         catch (OperationCanceledException)
         {
            //Task cancelled, legit, ignoring exception.
         }
         catch (Exception)
         {
            this.logger.LogDebug("Unexpected Exception during statistic generation, Statistic Collector stopped.");
         }
      }

      /// <summary>
      /// Creates the table builder for a specific <see cref="StatisticFeedDefinition" />.
      /// </summary>
      /// <param name="definition">The definition.</param>
      /// <returns></returns>
      private TableBuilder CreateTableBuilder(StatisticFeedDefinition definition)
      {
         var builder = new TableBuilder(this.writer);

         foreach (FieldDefinition field in definition.FieldsDefinition)
         {
            builder.AddColumn(new ColumnDefinition { Label = field.Label, Width = field.WidthHint, Alignment = ColumnAlignment.Left });
         }

         return builder.Prepare();
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      /// <summary>
      /// Gets an anonymous object containing the feeds dump.
      /// </summary>
      /// <returns></returns>
      public object GetFeedsDump()
      {
         return System.Text.Json.JsonSerializer.Serialize(
            from feed in this.registeredfeedDefinitions
               //from feedRows in feed.LastResults
            select new
            {
               Title = feed.StatisticFeedDefinition.Title,
               Labels = from fieldDefinition in feed.StatisticFeedDefinition.FieldsDefinition select fieldDefinition.Label,
               Values = feed.LastResults
            }//, new System.Text.Json.JsonSerializerOptions {  WriteIndented=}
         );
      }
   }
}
