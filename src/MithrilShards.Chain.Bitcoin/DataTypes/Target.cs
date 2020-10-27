using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MithrilShards.Chain.Bitcoin.Converters;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   [TypeConverter(typeof(TargetConverter))]
   public partial class Target : UInt256
   {
      private static BigInteger Pow256 = BigInteger.Pow(new BigInteger(2), 256);

      public static new Target Zero { get; } = new Target("0".PadRight(EXPECTED_SIZE * 2, '0'));

      /// <summary>
      /// Returns a Target representing the passed raw value.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns></returns>
      public static Target FromRawValue(ulong value)
      {
         return new Target { part1 = value };
      }

      private Target() { }

      public Target(string hexString) : base(hexString) { }

      /// <summary>
      /// Initializes a new instance of the <see cref="Target"/> class from a raw byte array.
      /// </summary>
      /// <param name="input"></param>
      public Target(ReadOnlySpan<byte> input) : base(input) { }

      /// <summary>
      /// Initializes a new instance of the <see cref="Target"/> class from a compact value (big-endian representation).
      /// </summary>
      /// <param name="compactValue">The compact value.</param>
      public Target(uint compactValue)
      {
         Span<byte> data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));

         byte exponent = (byte)(compactValue >> 24); // number of bytes of N
         uint mantissa = compactValue & 0x007fffff;

         if (exponent <= 3)
         {
            mantissa >>= 8 * (3 - exponent);
            //this.part1 = mantissa;
            BinaryPrimitives.WriteUInt32LittleEndian(data, mantissa);
         }
         else
         {
            this.part1 = mantissa;
            this.ShiftLeft(8 * (exponent - 3));
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="Target"/> class from a compact value (big-endian representation).
      /// </summary>
      /// <param name="compactValue">The compact value.</param>
      public Target(uint compactValue, out bool isNegative, out bool isOverflow)
      {
         Span<byte> data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));

         byte exponent = (byte)(compactValue >> 24); // number of bytes of N
         uint mantissa = compactValue & 0x007fffff;

         if (exponent <= 3)
         {
            mantissa >>= 8 * (3 - exponent);
            BinaryPrimitives.WriteUInt32LittleEndian(data, mantissa);
         }
         else
         {
            this.part1 = mantissa;
            this.ShiftLeft(8 * (exponent - 3));
         }

         // 0x00800000 is the mask to use to obtain the sign.
         isNegative = mantissa != 0 && (compactValue & 0x00800000) != 0;

         isOverflow = mantissa != 0 && ((exponent > 34) ||
                                       (mantissa > 0xff && exponent > 33) ||
                                       (mantissa > 0xffff && exponent > 32));
      }

      public int Bits()
      {
         const int bitsPerPart = sizeof(ulong) * 8;

         if (this.part4 != 0)
         {
            int zeroes = BitOperations.LeadingZeroCount(this.part4);
            if (zeroes > 0) return (bitsPerPart * 4) - zeroes;
         }

         if (this.part3 != 0)
         {
            int zeroes = BitOperations.LeadingZeroCount(this.part3);
            if (zeroes > 0) return (bitsPerPart * 3) - zeroes;
         }

         if (this.part2 != 0)
         {
            int zeroes = BitOperations.LeadingZeroCount(this.part2);
            if (zeroes > 0) return (bitsPerPart * 2) - zeroes;
         }

         if (this.part1 != 0)
         {
            int zeroes = BitOperations.LeadingZeroCount(this.part1);
            if (zeroes > 0) return bitsPerPart - zeroes;
         }

         return 0;
      }

      public uint ToCompact(bool isNegative = false)
      {
         uint compact;

         int size = (this.Bits() + 7) / 8;
         if (size <= 3)
         {
            compact = (uint)(this.part1 << 8 * (3 - size));
         }
         else
         {
            compact = this.GetCompactMantissa(8 * (size - 3));
         }

         // The 0x00800000 bit denotes the sign.
         // Thus, if it is already set, divide the mantissa by 256 and increase the exponent.
         if ((compact & 0x00800000) != 0)
         {
            compact >>= 8;
            size++;
         }

         Debug.Assert((compact & ~0x007fffff) == 0);
         Debug.Assert(size < 256);
         compact |= (uint)(size << 24);
         compact |= (uint)(isNegative && ((compact & 0x007fffff) != 0) ? 0x00800000 : 0);
         return compact;
      }

      public BigInteger ToBigInteger()
      {
         uint compact = ToCompact();
         var exp = compact >> 24;
         var value = compact & 0x00FFFFFF;
         return new BigInteger(value) << (8 * ((int)exp - 3));
      }

      /// <summary>
      /// Calculates the amount of work that this target, representing the difficulty of a block, contributes to the total chain work.
      /// </summary>
      /// <returns>
      /// Amount of work.
      /// </returns>
      public Target GetBlockProof()
      {
         // bitcoin core says: we need to compute 2**256 / (bnTarget+1), but we can't represent 2**256
         // as it's too large for an arith_uint256. However, as 2**256 is at least as large
         // as bnTarget+1, it is equal to ((2**256 - bnTarget - 1) / (bnTarget+1)) + 1,
         // or ~bnTarget / (bnTarget+1) + 1.

         //but we can using BigInteger (less performant, could be improved)

         var asBigInt = new BigInteger(this.GetBytes());
         if (this <= Zero || asBigInt >= Pow256)
            return Zero;


         var proof = new Target();
         Span<byte> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref proof.part1), EXPECTED_SIZE);
         data.Clear();
         (Pow256 / (asBigInt + 1)).TryWriteBytes(data, out _);

         return proof;
      }
   }
}
