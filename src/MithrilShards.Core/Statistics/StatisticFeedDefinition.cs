using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Statistics
{
   public class StatisticFeedDefinition
   {
      /// <summary>
      /// Gets the unique feed identifier.
      /// </summary>
      public string FeedId { get; }

      /// <summary>
      /// Gets the title of the feed.
      /// </summary>
      public string Title { get; }

      /// <summary>
      /// Gets the counter definitions composing a statistic feed.
      /// </summary>
      public List<FieldDefinition> FieldsDefinition { get; }

      /// <summary>
      /// Gets the target frequency of the statistic generation/dump.
      /// </summary>
      public TimeSpan FrequencyTarget { get; }

      public StatisticFeedDefinition(string feedId, string title, List<FieldDefinition> counterDefinitions, TimeSpan frequencyTarget)
      {
         this.FeedId = feedId;
         this.Title = title ?? throw new ArgumentNullException(nameof(title));
         this.FieldsDefinition = counterDefinitions ?? throw new ArgumentNullException(nameof(title));
         this.FrequencyTarget = frequencyTarget;
      }
   }
}