using System;

namespace MithrilShards.Logging.TableFormatter;

public static class StringExtensions
{
   public static string Align(this string text, int totalWidth, char paddingCharacter = ' ')
   {
      return totalWidth > 0 ? text.PadRight(totalWidth, paddingCharacter) : text.PadLeft(Math.Abs(totalWidth), paddingCharacter);
   }

   public static string AlignLeft(this string text, int totalWidth, char paddingCharacter = ' ')
   {
      return text.PadRight(Math.Abs(totalWidth), paddingCharacter);
   }

   public static string AlignRight(this string text, int totalWidth, char paddingCharacter = ' ')
   {
      return text.PadLeft(Math.Abs(totalWidth), paddingCharacter);
   }

   // Define other methods and classes here
   public static string Center(this string text, int totalWidth, char paddingCharacter = ' ')
   {
      return text
          .PadLeft((totalWidth + text.Length) / 2, paddingCharacter)
          .PadRight(totalWidth, paddingCharacter);
   }
}
