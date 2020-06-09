using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      public static new Target Zero { get; } = new Target("0".PadRight(EXPECTED_SIZE * 2, '0'));

      private Target() { }

      public Target(string hexString) : base(hexString) { }

      public Target(ReadOnlySpan<byte> input) : base(input) { }

      public Target(uint compactValue)
      {
         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));

         byte exponent = (byte)(compactValue >> 24); // number of bytes of N
         uint mantissa = compactValue & 0x007fffff;

         /// 0x00800000 is the mask to use to obtain the sign, if needed.
         /// Actually this type is only used to express the difficult Target so it's not needed.

         //if (exponent <= 3)
         //{

         //   mantissa >>= 8 * (3 - exponent);
         //   *this = mantissa;
         //}
         //else
         //{
         //   *this = mantissa;
         //   *this <<= 8 * (exponent - 3);
         //}
         //if (pfNegative)
         //   *pfNegative = mantissa != 0 && (compactValue & 0x00800000) != 0;
         //if (pfOverflow)
         //   *pfOverflow = mantissa != 0 && ((exponent > 34) ||
         //                                (mantissa > 0xff && exponent > 33) ||
         //                                (mantissa > 0xffff && exponent > 32));
         //return *this;
      }

      /// <summary>
      /// Returns a Target representing the passed value.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns></returns>
      public static Target FromValue(ulong value)
      {
         return new Target
         {
            part1 = value
         };
      }


      public int Bits()
      {
         const int bitsPerPart = sizeof(ulong);

         int zeroes = BitOperations.TrailingZeroCount(this.part4);
         if (zeroes > 0) return (bitsPerPart * 4) - zeroes;

         zeroes = BitOperations.TrailingZeroCount(this.part3);
         if (zeroes > 0) return (bitsPerPart * 3) - zeroes;

         zeroes = BitOperations.TrailingZeroCount(this.part2);
         if (zeroes > 0) return (bitsPerPart * 2) - zeroes;

         zeroes = BitOperations.TrailingZeroCount(this.part1);
         if (zeroes > 0) return bitsPerPart - zeroes;

         return 0;
      }
   }
}
