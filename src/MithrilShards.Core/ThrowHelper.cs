using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MithrilShards.Core
{

   /// <summary>
   /// Helper to throw exception, allowing caller to have a higher chance to be inlined.
   /// As per dotnet/runtime repo:
   /// 1. Extracting the throw makes the method preforming the throw in a conditional branch smaller and more inlinable
   /// 2. Extracting the throw from generic method to non-generic method reduces the repeated codegen size for value types
   /// 3. Newer JITs will not inline the methods that only throw and also recognise them, move the call to cold section
   /// and not add stack prep and unwind before calling https://github.com/dotnet/coreclr/pull/6103
   /// </summary>
   public static class ThrowHelper
   {
      [DoesNotReturn]
      public static void ThrowFormatException(string message)
      {
         throw new FormatException(message);
      }

      [DoesNotReturn]
      public static void ThrowArgumentNullException(string fieldName)
      {
         throw new ArgumentNullException(fieldName);
      }
      
      [DoesNotReturn]
      public static void ThrowArgumentException(string message)
      {
         throw new ArgumentException(message);
      }
   }
}
