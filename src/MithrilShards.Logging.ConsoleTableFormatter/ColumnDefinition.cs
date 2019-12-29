using System;

namespace MithrilShards.Logging.ConsoleTableFormatter {
   public class ColumnDefinition {
      public string Label { get; set; }
      public int Width { get; set; }
      public ColumnAlignment Alignment { get; set; }

      public ColumnDefinition() {
         this.Label = String.Empty;
         this.Width = 20;
         this.Alignment = ColumnAlignment.Left;
      }
   }
}
