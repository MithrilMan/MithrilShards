namespace MithrilShards.Logging.TableFormatter
{
   public class TableStyle
   {
      public char Top { get; }
      public char Right { get; }
      public char Bottom { get; }
      public char Left { get; }
      public string Separator { get; }
      public bool TitleBorder { get; }
      public ColumnAlignment TitleAlignment { get; }

      public TableStyle(char top = '-',
                        char right = '|',
                        char bottom = '-',
                        char left = '|',
                        string separator = " | ",
                        bool titleBorder = false,
                        ColumnAlignment titleAlignment = ColumnAlignment.Center)
      {
         this.Top = top;
         this.Right = right;
         this.Bottom = bottom;
         this.Left = left;
         this.Separator = separator;
         this.TitleBorder = titleBorder;
         this.TitleAlignment = titleAlignment;
      }
   }
}
