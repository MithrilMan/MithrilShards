using System;

namespace MithrilShards.Logging.TableFormatter
{
   public class ColumnDefinition
   {
      public string Label { get; set; }
      public int Width { get; set; }
      public ColumnAlignment Alignment { get; set; }

      public ColumnDefinition()
      {
         Label = String.Empty;
         Width = 20;
         Alignment = ColumnAlignment.Left;
      }
   }
}
