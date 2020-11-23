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
      private readonly TableBuilder _tableBuilder;


      /// <summary>
      /// The string builder that will hold human readable output;
      /// </summary>
      private readonly StringBuilder _stringBuilder = new StringBuilder();

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
      public DateTimeOffset LastResultsDate { get; internal set; }

      /// <summary>
      /// The feed has a result.
      /// </summary>
      private bool _hasResult = false;

      public ScheduledStatisticFeed(IStatisticFeedsProvider source, StatisticFeedDefinition statisticFeedDefinition)
      {
         Source = source ?? throw new ArgumentNullException(nameof(source));
         StatisticFeedDefinition = statisticFeedDefinition ?? throw new ArgumentNullException(nameof(statisticFeedDefinition));
         NextPlannedExecution = DateTime.Now + statisticFeedDefinition.FrequencyTarget;

         _tableBuilder = CreateTableBuilder();
      }

      /// <summary>
      /// Creates the table builder for a specific <see cref="StatisticFeedDefinition" />.
      /// </summary>
      /// <param name="definition">The definition.</param>
      /// <returns></returns>
      private TableBuilder CreateTableBuilder()
      {
         var tableBuilder = new TableBuilder(_stringBuilder);

         foreach (FieldDefinition field in StatisticFeedDefinition.FieldsDefinition)
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
            Title = StatisticFeedDefinition.Title,
            Time = LastResultsDate,
            Labels = from fieldDefinition in StatisticFeedDefinition.FieldsDefinition select fieldDefinition.Label,
            Values = lastResults
         });
      }

      public void SetLastResults(IEnumerable<string?[]> results)
      {
         lastResults.Clear();
         lastResults.AddRange(results);
         LastResultsDate = DateTime.Now;
         _hasResult = true;
      }


      /// <summary>
      /// Gets the feed in a textual tabular format.
      /// </summary>
      /// <returns></returns>
      public string GetTabularFeed()
      {
         _stringBuilder.Clear();

         _tableBuilder.Start($"{LastResultsDate.LocalDateTime} - {StatisticFeedDefinition.Title}");
         if (_hasResult)
         {
            lastResults.ForEach(row => _tableBuilder.DrawRow(row));
         }
         else
         {
            _stringBuilder.AppendLine("No statistics available yet.");
         }

         _tableBuilder.End();


         return _stringBuilder.ToString();
      }
   }
}
