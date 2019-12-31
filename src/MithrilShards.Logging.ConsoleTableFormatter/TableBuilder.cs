﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MithrilShards.Logging.ConsoleTableFormatter {
   public class TableBuilder {
      private readonly OutputWriter writer;
      private bool prepared;

      public List<ColumnDefinition> ColumnDefinitions { get; }
      public TableStyle TableStyle { get; private set; }
      public int Width { get; private set; }

      public TableBuilder(OutputWriter writer) {
         this.writer = writer ?? throw new ArgumentNullException(nameof(writer));

         this.ColumnDefinitions = new List<ColumnDefinition>();
      }

      public TableBuilder AddColumns(params ColumnDefinition[] columns) {
         if (this.prepared) {
            throw new TableAlreadyDefinedException();
         }

         this.ColumnDefinitions.AddRange(columns);
         return this;
      }

      public TableBuilder AddColumn(ColumnDefinition column) {
         if (this.prepared) {
            throw new TableAlreadyDefinedException();
         }

         this.ColumnDefinitions.Add(column);
         return this;
      }

      public TableBuilder SetStyle(TableStyle style) {
         if (this.prepared) {
            throw new TableAlreadyDefinedException();
         }

         this.TableStyle = style;
         return this;
      }

      /// <summary>
      /// Prepares this instance.
      /// After having called this method, no alteration could be made on the table definition and only rows can be added.
      /// </summary>
      /// <returns></returns>
      public TableBuilder Prepare() {
         if (this.prepared) {
            throw new TableAlreadyDefinedException();
         }

         this.prepared = true;
         if (this.TableStyle == null) {
            this.TableStyle = new TableStyle();
         }

         this.ComputeTableWidth();
         return this;
      }

      public TableBuilder Start(string title = null) {
         if (!this.prepared) {
            this.Prepare();
         }

         if (title != null) {
            this.DrawTitle(title);
         }

         this.DrawTopBorder();

         this.DrawLeftBorder();
         for (int i = 0; i < this.ColumnDefinitions.Count; i++) {
            this.DrawColumn(i, this.ColumnDefinitions[i].Label.Center(this.ColumnDefinitions[i].Width));
         }
         this.DrawRightBorder();

         this.DrawTopBorder();

         return this;
      }

      private void DrawTitle(string title) {
         if (this.TableStyle.TitleBorder) {
            this.DrawTopBorder();
            this.DrawLeftBorder();
            int availableSpace = this.Width - (this.TableStyle.Left == null ? 0 : 1) - (this.TableStyle.Right == null ? 0 : 1);
            this.writer.Write(title.Center(availableSpace));
            this.DrawRightBorder();
         }
         else {
            this.writer.WriteLine(title.Center(this.Width));
         }
      }

      public TableBuilder DrawRow(params string[] values) {
         if (values is null) {
            throw new ArgumentNullException(nameof(values));
         }

         if (values.Length > this.ColumnDefinitions.Count) {
            throw new ArgumentOutOfRangeException("values length is greater than column lengths.");
         }

         this.DrawLeftBorder();
         for (int i = 0; i < this.ColumnDefinitions.Count; i++) {
            if (i < values.Length) {
               this.DrawColumn(i, values[i]);
            }
            else {
               this.DrawColumn(i, string.Empty);
            }
         }
         this.DrawRightBorder();

         return this;
      }

      public TableBuilder End() {
         this.DrawBottomBorder();
         this.writer.WriteLine();

         return this;
      }

      private void ComputeTableWidth() {
         this.Width =
             (this.TableStyle.Left == null ? 0 : 1) // length of the left border
             + this.ColumnDefinitions.Sum(cd => cd.Width) //sum of the column widths
             + ((this.ColumnDefinitions.Count - 1) * this.TableStyle.Separator.Length) // sum of the space occupied by column separators;
             + (this.TableStyle.Right == null ? 0 : 1)// length of the right border
             ;
      }

      private void DrawTopBorder() {
         if (this.TableStyle.Top != null) {
            this.writer.DrawLine(this.TableStyle.Top, this.Width);
         }
      }

      private void DrawLeftBorder() {
         if (this.TableStyle.Left != null) {
            this.writer.Write(this.TableStyle.Left.ToString());
         }
      }

      private void DrawColumn(int columnIndex, string value) {
         ColumnDefinition columnDefinition = this.ColumnDefinitions[columnIndex];

         switch (columnDefinition.Alignment) {
            case ColumnAlignment.Left:
               this.writer.Write(value.AlignLeft(columnDefinition.Width));
               break;
            case ColumnAlignment.Right:
               this.writer.Write(value.AlignRight(columnDefinition.Width));
               break;
            case ColumnAlignment.Center:
               this.writer.Write(value.Center(columnDefinition.Width));
               break;
            default:
               throw new NotImplementedException(columnDefinition.Alignment.ToString());
         }

         bool isLastColumn = columnIndex == this.ColumnDefinitions.Count - 1;
         if (!isLastColumn) {
            this.DrawColumnSeparator();
         }
      }

      private void DrawColumnSeparator() {
         if (this.TableStyle.Separator.Length > 0) {
            this.writer.Write(this.TableStyle.Separator.ToString());
         }
      }

      private void DrawRightBorder() {
         if (this.TableStyle.Right != null) {
            this.writer.WriteLine(this.TableStyle.Right.ToString());
         }
         else {
            this.writer.WriteLine();
         }
      }

      private void DrawBottomBorder() {
         if (this.TableStyle.Bottom != null) {
            this.writer.DrawLine(this.TableStyle.Bottom, this.Width);
         }
      }
   }
}