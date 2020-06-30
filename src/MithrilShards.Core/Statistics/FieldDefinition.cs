using System;
using System.Text.Json.Serialization;

namespace MithrilShards.Core.Statistics
{
   public class FieldDefinition
   {

      /// <summary>
      /// Gets the Counter label.
      /// </summary>
      /// <value>
      /// The label.
      /// </value>
      public string Label { get; }
      public string Description { get; }

      /// <summary>
      /// Gets the width hint (expected value length).
      /// </summary>
      /// <value>
      /// The width hint.
      /// </value>
      public int WidthHint { get; }

      /// <summary>
      /// Gets the unit of measure of the counter.
      /// </summary>
      /// <value>
      /// The unit of measure.
      /// </value>
      public string? UnitOfMeasure { get; }

      [JsonIgnore]
      public Func<(object? value, int widthHint), string>? ValueFormatter { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="FieldDefinition"/> class.
      /// </summary>
      /// <param name="label">The label.</param>
      /// <param name="widthHint">The width hint.</param>
      /// <param name="unitOfMeasure">The unit of measure.</param>
      /// <param name="valueFormatter">The value formatter (null if value doesn't need to be formatted).</param>
      public FieldDefinition(string label, string description, int widthHint, string? unitOfMeasure = null, Func<(object? value, int widthHint), string>? valueFormatter = null)
      {
         this.Label = label ?? throw new ArgumentNullException(nameof(label));
         this.Description = description ?? throw new ArgumentNullException(nameof(description));
         this.WidthHint = widthHint;
         this.UnitOfMeasure = unitOfMeasure;
         this.ValueFormatter = valueFormatter;
      }
   }
}