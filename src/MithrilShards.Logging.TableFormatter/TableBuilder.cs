using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MithrilShards.Logging.TableFormatter;

public class TableBuilder
{
   private readonly StringBuilder _stringBuilder;
   private bool _prepared;

   public List<ColumnDefinition> ColumnDefinitions { get; }
   public TableStyle TableStyle { get; private set; } = null!;
   public int Width { get; private set; }

   public TableBuilder(StringBuilder builder)
   {
      _stringBuilder = builder ?? new StringBuilder();

      ColumnDefinitions = new List<ColumnDefinition>();
   }

   public TableBuilder AddColumns(params ColumnDefinition[] columns)
   {
      if (_prepared)
      {
         throw new TableAlreadyDefinedException();
      }

      ColumnDefinitions.AddRange(columns);
      return this;
   }

   public TableBuilder AddColumn(ColumnDefinition column)
   {
      if (_prepared)
      {
         throw new TableAlreadyDefinedException();
      }

      ColumnDefinitions.Add(column);
      return this;
   }

   public TableBuilder SetStyle(TableStyle style)
   {
      if (_prepared)
      {
         throw new TableAlreadyDefinedException();
      }

      TableStyle = style;
      return this;
   }

   /// <summary>
   /// Prepares this instance.
   /// After having called this method, no alteration could be made on the table definition and only rows can be added.
   /// </summary>
   /// <returns></returns>
   public TableBuilder Prepare()
   {
      if (_prepared)
      {
         throw new TableAlreadyDefinedException();
      }

      _prepared = true;
      if (TableStyle == null)
      {
         TableStyle = new TableStyle();
      }

      ComputeTableWidth();
      return this;
   }

   public TableBuilder Start(string? title = null)
   {
      if (!_prepared)
      {
         Prepare();
      }

      if (title != null)
      {
         DrawTitle(title);
      }

      DrawTopBorder();

      DrawLeftBorder();
      for (int i = 0; i < ColumnDefinitions.Count; i++)
      {
         DrawColumn(i, ColumnDefinitions[i].Label.Center(ColumnDefinitions[i].Width));
      }
      DrawRightBorder();

      DrawTopBorder();

      return this;
   }

   private void DrawTitle(string title)
   {
      string alignTitle(ColumnAlignment alignment, int availableSpace) => alignment switch
      {
         ColumnAlignment.Left => title.AlignLeft(availableSpace),
         ColumnAlignment.Right => title.AlignRight(availableSpace),
         ColumnAlignment.Center => title.Center(availableSpace),
         _ => string.Empty
      };

      if (TableStyle.TitleBorder)
      {
         DrawTopBorder();
         DrawLeftBorder();
         int availableSpace = Width - (TableStyle.Left == char.MinValue ? 0 : 1) - (TableStyle.Right == char.MinValue ? 0 : 1);
         _stringBuilder.AppendLine(alignTitle(TableStyle.TitleAlignment, availableSpace));
         DrawRightBorder();
      }
      else
      {
         _stringBuilder.AppendLine(alignTitle(TableStyle.TitleAlignment, Width));
      }
   }

   public TableBuilder DrawRow(string?[] values)
   {
      if (values is null) throw new ArgumentNullException(nameof(values));

      if (values.Length > ColumnDefinitions.Count)
      {
         throw new ArgumentOutOfRangeException(nameof(values), "values length is greater than column lengths.");
      }

      DrawLeftBorder();
      for (int i = 0; i < ColumnDefinitions.Count; i++)
      {
         if (i < values.Length)
         {
            DrawColumn(i, values[i] ?? string.Empty);
         }
         else
         {
            DrawColumn(i, string.Empty);
         }
      }
      DrawRightBorder();

      return this;
   }

   public TableBuilder End()
   {
      DrawBottomBorder();
      return this;
   }

   private void ComputeTableWidth()
   {
      Width =
          (TableStyle.Left == char.MinValue ? 0 : 1) // length of the left border
          + ColumnDefinitions.Sum(cd => cd.Width) //sum of the column widths
          + ((ColumnDefinitions.Count - 1) * TableStyle.Separator.Length) // sum of the space occupied by column separators;
          + (TableStyle.Right == char.MinValue ? 0 : 1)// length of the right border
          ;
   }

   private void DrawTopBorder()
   {
      if (TableStyle.Top != char.MinValue)
      {
         DrawLine(TableStyle.Top, Width);
      }
   }

   private void DrawLeftBorder()
   {
      if (TableStyle.Left != char.MinValue)
      {
         _stringBuilder.Append(TableStyle.Left.ToString(CultureInfo.InvariantCulture));
      }
   }

   private void DrawColumn(int columnIndex, string value)
   {
      ColumnDefinition columnDefinition = ColumnDefinitions[columnIndex];

      switch (columnDefinition.Alignment)
      {
         case ColumnAlignment.Left:
            _stringBuilder.Append(value.AlignLeft(columnDefinition.Width));
            break;
         case ColumnAlignment.Right:
            _stringBuilder.Append(value.AlignRight(columnDefinition.Width));
            break;
         case ColumnAlignment.Center:
            _stringBuilder.Append(value.Center(columnDefinition.Width));
            break;
         default:
            throw new NotImplementedException(columnDefinition.Alignment.ToString());
      }

      bool isLastColumn = columnIndex == ColumnDefinitions.Count - 1;
      if (!isLastColumn)
      {
         DrawColumnSeparator();
      }
   }

   private void DrawColumnSeparator()
   {
      if (TableStyle.Separator.Length > 0)
      {
         _stringBuilder.Append(TableStyle.Separator);
      }
   }

   private void DrawRightBorder()
   {
      if (TableStyle.Right != char.MinValue)
      {
         _stringBuilder.AppendLine(TableStyle.Right.ToString(CultureInfo.InvariantCulture));
      }
      else
      {
         _stringBuilder.AppendLine();
      }
   }

   private void DrawBottomBorder()
   {
      if (TableStyle.Bottom != char.MinValue)
      {
         DrawLine(TableStyle.Bottom, Width);
      }
   }

   public void DrawLine(char character, int width)
   {
      _stringBuilder.AppendLine(string.Empty.PadRight(width, character));
   }
}
