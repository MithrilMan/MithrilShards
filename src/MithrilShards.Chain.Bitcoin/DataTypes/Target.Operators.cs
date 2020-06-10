using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      const int UINT_ELEMENTS_COUNT = EXPECTED_SIZE / sizeof(uint);
      const int UINT_BIT_SIZE = sizeof(uint) * 8;

      public static Target operator +(Target left, Target right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(result);

         ulong carry = 0;
         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            ulong n = carry + result[i] + rightBytes[i];
            result[i] = (uint)(n & 0xffffffff);
            carry = n >> UINT_BIT_SIZE;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator -(Target item)
      {
         return new Target
         {
            part1 = ~item.part1,
            part2 = ~item.part2,
            part3 = ~item.part3,
            part4 = ~item.part4,
         };
      }

      public static Target operator -(Target left, Target right)
      {
         return left + -right;
      }

      public static Target operator <<(Target left, int shiftAmount)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> leftCopy = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(leftCopy);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / UINT_BIT_SIZE;
         shiftAmount %= UINT_BIT_SIZE;

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i + k + 1 < UINT_ELEMENTS_COUNT && shiftAmount != 0)
               result[i + k + 1] |= leftCopy[i] >> (UINT_BIT_SIZE - shiftAmount);

            if (i + k < UINT_ELEMENTS_COUNT)
               result[i + k] |= leftCopy[i] << shiftAmount;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator >>(Target left, int shiftAmount)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> leftCopy = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(leftCopy);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / UINT_BIT_SIZE;
         shiftAmount %= UINT_BIT_SIZE;

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i + k + 1 < UINT_ELEMENTS_COUNT && shiftAmount != 0)
               result[i + k + 1] |= leftCopy[i] << (UINT_BIT_SIZE - shiftAmount);

            if (i + k < UINT_ELEMENTS_COUNT)
               result[i + k] |= leftCopy[i] >> shiftAmount;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }


      //public static Target operator +(Target? a, Target? b)
      //{
      //   // todo
      //   return a + b;
      //}

      public static Target operator *(Target left, Target right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];

         for (int j = 0; j < UINT_ELEMENTS_COUNT; j++)
         {
            ulong carry = 0;
            for (int i = 0; i + j < UINT_ELEMENTS_COUNT; i++)
            {
               ulong n = carry + result[i + j] + ((ulong)leftBytes[j] * rightBytes[i]);
               result[i + j] = (uint)(n & 0xffffffff);
               carry = n >> UINT_BIT_SIZE;
            }
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator *(Target left, uint right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];

         ulong carry = 0;
         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            ulong n = carry + ((ulong)leftBytes[i] * right);
            result[i] = (uint)(n & 0xffffffff);
            carry = n >> UINT_BIT_SIZE;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator /(Target numerator, Target denominator)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref numerator.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref denominator.part1), UINT_ELEMENTS_COUNT);

         int numeratorBits = numerator.Bits();
         int denominatorBits = denominator.Bits();

         if (denominatorBits == 0)
         {
            ThrowHelper.ThrowArgumentException("Division by zero");
         }

         // the result is certainly 0.
         if (denominatorBits > numeratorBits)
         {
            return Zero;
         }

         // the quotient.
         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         //result.Fill(0); //allocated data is already zeroed

         int shiftAmount = numeratorBits - denominatorBits;
         denominator <<= shiftAmount; // shift so that div and num align.
         while (shiftAmount >= 0)
         {
            if (numerator >= denominator)
            {
               numerator -= denominator;
               // set a bit of the result.
               result[shiftAmount / UINT_BIT_SIZE] |= (uint)(1 << (shiftAmount & (UINT_BIT_SIZE - 1)));
            }

            denominator >>= 1; // shift back.
            shiftAmount--;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator *(Target left, ulong right)
      {
         return left * FromRawValue(right);
      }

      public static Target operator /(Target left, ulong right)
      {
         return left / FromRawValue(right);
      }
   }
}