using System;

namespace MithrilShards.Core.Encoding
{
   public static class HexEncoder
   {
      const string HexValues = "0123456789ABCDEF";

      public static string ToHexString(ReadOnlySpan<byte> rawData, bool reverse = false)
      {
         int length = rawData.Length;
         return string.Create(2 * rawData.Length, rawData.ToArray(), (dst, src) =>
         {
            string hexDigits = HexValues; //JIT optimization
            int i = src.Length - 1;

            if (reverse)
            {
               int j = 0;

               while (i >= 0)
               {
                  byte b = src[i--];
                  dst[j++] = hexDigits[b >> 4];
                  dst[j++] = hexDigits[b & 0xF];
               }
            }
            else
            {
               int j = (src.Length * 2) - 1;

               while (i >= 0)
               {
                  byte b = src[i--];
                  dst[j--] = hexDigits[b >> 4];
                  dst[j--] = hexDigits[b & 0xF];
               }
            }
         });
      }
   }
}
