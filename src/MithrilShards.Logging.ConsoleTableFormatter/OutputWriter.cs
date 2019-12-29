using System;

namespace MithrilShards.Logging.ConsoleTableFormatter {
   public class OutputWriter {
      public const int ConsoleWidth = 80;

      /// <summary>
      /// Produces a text.
      /// </summary>
      /// <param name="text">The text.</param>
      public delegate void Writer(string text);

      private readonly Writer writer;

      public OutputWriter(Writer writer = null) {
         this.writer = writer ?? Console.Write;
      }

      public OutputWriter Write(string text) {
         this.writer(text);

         return this;
      }

      public OutputWriter WriteLine(string text = null) {
         this.writer(text + '\n');

         return this;
      }

      public OutputWriter DrawLine(char? character = '-', int lenght = ConsoleWidth) {
         if (character == null) {
            return this;
         }

         _ = this.WriteLine(string.Empty.PadRight(lenght, character.Value));

         return this;
      }
   }
}
