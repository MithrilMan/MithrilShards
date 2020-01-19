using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MithrilShards.Core.Statistics;
using MithrilShards.Logging.TableFormatter;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class ScheduledStatisticFeed
   {
      /// <summary>
      /// Gets the table builder used to generate a tabular output.
      /// </summary>
      /// <value>
      /// The table builder.
      /// </value>
      private readonly TableBuilder tableBuilder;


      /// <summary>
      /// The string builder that will hold human readable output;
      /// </summary>
      private readonly StringBuilder stringBuilder = new StringBuilder();

      /// <summary>
      /// Gets the source of the feed.
      /// </summary>
      /// <value>
      /// The source.
      /// </value>
      public IStatisticFeedsProvider Source { get; }

      /// <summary>
      /// Gets the statistic feed definition.
      /// </summary>
      /// <value>
      /// The statistic feed definition.
      /// </value>
      public StatisticFeedDefinition StatisticFeedDefinition { get; }

      /// <summary>
      /// Gets the next planned execution time.
      /// </summary>
      /// <value>
      /// The next planned execution.
      /// </value>
      public DateTimeOffset NextPlannedExecution { get; internal set; }

      /// <summary>Last obtained result.</summary>
      public List<string?[]> lastResults { get; } = new List<string?[]>();

      /// <summary>
      /// Gets the last result date.
      /// </summary>
      /// <value>
      /// The last result date.
      /// </value>
      public DateTimeOffset LastResultDate { get; internal set; }

      public ScheduledStatisticFeed(IStatisticFeedsProvider source, StatisticFeedDefinition statisticFeedDefinition)
      {
         this.Source = source ?? throw new ArgumentNullException(nameof(source));
         this.StatisticFeedDefinition = statisticFeedDefinition ?? throw new ArgumentNullException(nameof(statisticFeedDefinition));
         this.NextPlannedExecution = DateTime.Now + statisticFeedDefinition.FrequencyTarget;

         this.tableBuilder = this.CreateTableBuilder();
      }

      /// <summary>
      /// Creates the table builder for a specific <see cref="StatisticFeedDefinition" />.
      /// </summary>
      /// <param name="definition">The definition.</param>
      /// <returns></returns>
      private TableBuilder CreateTableBuilder()
      {
         var tableBuilder = new TableBuilder(this.stringBuilder);

         foreach (FieldDefinition field in this.StatisticFeedDefinition.FieldsDefinition)
         {
            tableBuilder.AddColumn(new ColumnDefinition { Label = field.Label, Width = field.WidthHint, Alignment = ColumnAlignment.Left });
         }

         tableBuilder.Prepare();
         return tableBuilder;
      }


      /// <summary>
      /// Gets an anonymous object containing the feed dump.
      /// </summary>
      /// <returns></returns>
      public object GetLastResultsDump()
      {
         return System.Text.Json.JsonSerializer.Serialize(new
         {
            Title = this.StatisticFeedDefinition.Title,
            Time = this.LastResultDate,
            Labels = from fieldDefinition in this.StatisticFeedDefinition.FieldsDefinition select fieldDefinition.Label,
            Values = this.lastResults
         });
      }

      public void SetLastResults(IEnumerable<string?[]> results)
      {
         this.lastResults.Clear();
         this.lastResults.AddRange(results);
         this.LastResultDate = DateTime.Now;
      }

      /// <summary>
      /// Updates the table builder using last fetched results.
      /// </summary>
      /// <exception cref="NotImplementedException"></exception>
      public string GetHumanReadableFeed()
      {
         this.stringBuilder.Clear();
         this.tableBuilder.Start($"{this.LastResultDate.TimeOfDay} - {this.StatisticFeedDefinition.Title}");
         this.lastResults.ForEach(row => this.tableBuilder.DrawRow(row));
         this.tableBuilder.End();
         return this.stringBuilder.ToString();
      }
   }
}
