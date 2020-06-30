using System;

namespace MithrilShards.Core.Utils
{
   public class ByteSizeFormatter
   {
      private const string SI_UNITS = "kMGTPE";
      private const string UNITS = "KMGTPE";

      public static string HumanReadable(long bytes, bool si = true)
      {
         int unit = si ? 1000 : 1024;

         if (bytes < unit)
         {
            return $"{bytes} B";
         }

         var exp = (int)(Math.Log(bytes) / Math.Log(unit));
         var value = bytes / Math.Pow(unit, exp);

         return si ? $"{value:F2} {SI_UNITS[exp - 1]}B" : $"{value:F2} {UNITS[exp - 1]}iB";
      }
   }
}